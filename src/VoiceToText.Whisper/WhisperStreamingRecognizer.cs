using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceToText.Abstractions;
using VoiceToText.Audio;
using VoiceToText.Models;
using Whisper.net;

namespace VoiceToText.Whisper;

/// <summary>
/// Simulated streaming recognizer powered by Whisper.net.
/// Whisper is a batch model — streaming is simulated by accumulating audio
/// in a buffer and re-processing when the buffer exceeds <see cref="WhisperRecognizerOptions.StreamingBufferDuration"/>.
/// </summary>
public sealed class WhisperStreamingRecognizer : IStreamingRecognizer
{
    private readonly WhisperRecognizerOptions _options;
    private readonly ILogger<WhisperStreamingRecognizer> _logger;
    private WhisperFactory? _factory;

    private MemoryStream? _audioBuffer;
    private RecognizerOptions? _sessionOptions;
    private CancellationTokenSource? _cts;
    private Task? _processTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WhisperStreamingRecognizer"/>.
    /// </summary>
    public WhisperStreamingRecognizer(
        IOptions<WhisperRecognizerOptions> options,
        ILogger<WhisperStreamingRecognizer> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<StreamingRecognitionEventArgs>? PartialResultReceived;

    /// <inheritdoc />
    public event EventHandler<StreamingRecognitionEventArgs>? FinalResultReceived;

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public Task StartAsync(RecognizerOptions? options, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (IsListening)
            throw new InvalidOperationException("Streaming session is already active.");

        _sessionOptions = options;
        _audioBuffer = new MemoryStream();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsListening = true;

        _processTask = ProcessBufferLoopAsync(_cts.Token);

        _logger.LogDebug(
            "Whisper streaming session started (buffer: {BufferDuration}s)",
            _options.StreamingBufferDuration.TotalSeconds
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void PushAudio(ReadOnlySpan<byte> pcmData)
    {
        ThrowIfDisposed();

        if (!IsListening)
            throw new InvalidOperationException(
                "No active streaming session. Call StartAsync first."
            );

        lock (_audioBuffer!)
        {
            if (_audioBuffer.Length + pcmData.Length > _options.MaxStreamingBufferBytes)
                throw new InvalidOperationException(
                    $"Streaming audio buffer would exceed the maximum allowed size of {_options.MaxStreamingBufferBytes} bytes. "
                    + "Increase WhisperRecognizerOptions.MaxStreamingBufferBytes or process audio faster."
                );

            _audioBuffer.Write(pcmData);
        }
    }

    /// <inheritdoc />
    public void PushAudio(ReadOnlySpan<float> samples)
    {
        var pcm = AudioFormatConverter.FloatToPcm(samples);
        PushAudio(pcm.AsSpan());
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsListening)
            return;

        IsListening = false;

        // Cancel the processing loop
        _cts?.Cancel();

        // Wait for the loop to finish
        if (_processTask is not null)
        {
            try
            {
                await _processTask;
            }
            catch (OperationCanceledException) { }
        }

        // Process any remaining audio as a final result
        await ProcessRemainingAudioAsync();

        _audioBuffer?.Dispose();
        _audioBuffer = null;
        _cts?.Dispose();
        _cts = null;
        _processTask = null;

        _logger.LogDebug("Whisper streaming session stopped");
    }

    private async Task ProcessBufferLoopAsync(CancellationToken cancellationToken)
    {
        var bufferThresholdBytes = (int)(
            _options.StreamingBufferDuration.TotalSeconds * AudioConstants.DefaultByteRate
        );

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500, cancellationToken);

                long currentLength;
                lock (_audioBuffer!)
                {
                    currentLength = _audioBuffer.Length;
                }

                if (currentLength >= bufferThresholdBytes)
                {
                    await ProcessCurrentBufferAsync(isFinal: false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when StopAsync cancels the loop
        }
    }

    private async Task ProcessCurrentBufferAsync(bool isFinal)
    {
        byte[] audioData;
        lock (_audioBuffer!)
        {
            audioData = _audioBuffer.ToArray();
        }

        if (audioData.Length == 0)
            return;

        var factory = GetOrCreateFactory();
        using var processor = CreateProcessor(factory);
        using var stream = CreateWavStream(audioData);

        var lastText = string.Empty;

        await foreach (var segment in processor.ProcessAsync(stream))
        {
            lastText = segment.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(lastText))
            return;

        var args = new StreamingRecognitionEventArgs { Text = lastText, IsFinal = isFinal };

        if (isFinal)
            FinalResultReceived?.Invoke(this, args);
        else
            PartialResultReceived?.Invoke(this, args);
    }

    private async Task ProcessRemainingAudioAsync()
    {
        byte[] audioData;
        lock (_audioBuffer!)
        {
            audioData = _audioBuffer.ToArray();
        }

        if (audioData.Length == 0)
            return;

        var factory = GetOrCreateFactory();
        using var processor = CreateProcessor(factory);
        using var stream = CreateWavStream(audioData);

        var segments = new List<string>();

        await foreach (var segment in processor.ProcessAsync(stream))
        {
            segments.Add(segment.Text.Trim());
        }

        var fullText = string.Join(" ", segments.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            FinalResultReceived?.Invoke(
                this,
                new StreamingRecognitionEventArgs { Text = fullText, IsFinal = true }
            );
        }
    }

    /// <summary>
    /// Wraps raw 16kHz mono 16-bit PCM data in a WAV container so Whisper.net can parse it.
    /// </summary>
    private static MemoryStream CreateWavStream(byte[] pcmData)
    {
        const int headerSize = 44;
        var stream = new MemoryStream(headerSize + pcmData.Length);
        var header = new byte[headerSize];

        // RIFF header
        Encoding.ASCII.GetBytes("RIFF", header.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), headerSize - 8 + pcmData.Length);
        Encoding.ASCII.GetBytes("WAVE", header.AsSpan(8, 4));

        // fmt chunk
        Encoding.ASCII.GetBytes("fmt ", header.AsSpan(12, 4));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), 16); // chunk size
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(20), 1); // PCM format
        BinaryPrimitives.WriteInt16LittleEndian(
            header.AsSpan(22),
            (short)AudioConstants.DefaultChannels
        );
        BinaryPrimitives.WriteInt32LittleEndian(
            header.AsSpan(24),
            AudioConstants.DefaultSampleRate
        );
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), AudioConstants.DefaultByteRate);
        BinaryPrimitives.WriteInt16LittleEndian(
            header.AsSpan(32),
            (short)AudioConstants.DefaultBlockAlign
        );
        BinaryPrimitives.WriteInt16LittleEndian(
            header.AsSpan(34),
            (short)AudioConstants.DefaultBitsPerSample
        );

        // data chunk
        Encoding.ASCII.GetBytes("data", header.AsSpan(36, 4));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(40), pcmData.Length);

        stream.Write(header, 0, headerSize);
        stream.Write(pcmData, 0, pcmData.Length);
        stream.Position = 0;
        return stream;
    }

    private WhisperFactory GetOrCreateFactory()
    {
        if (_factory is not null)
            return _factory;

        if (string.IsNullOrWhiteSpace(_options.ModelPath))
            throw new InvalidOperationException("WhisperRecognizerOptions.ModelPath must be set to a non-empty path.");

        if (!File.Exists(_options.ModelPath))
            throw new InvalidOperationException($"Whisper model file does not exist: '{_options.ModelPath}'.");

        _logger.LogInformation("Loading Whisper model from {ModelPath}", _options.ModelPath);
        _factory = WhisperFactory.FromPath(_options.ModelPath);
        return _factory;
    }

    private WhisperProcessor CreateProcessor(WhisperFactory factory)
    {
        var builder = factory.CreateBuilder();

        var language = _sessionOptions?.Language ?? "auto";
        if (language == "auto")
            builder.WithLanguageDetection();
        else
            builder.WithLanguage(language);

        if (_options.Threads > 0)
            builder.WithThreads(_options.Threads);

        if (_options.Translate)
            builder.WithTranslate();

        if (_sessionOptions?.Prompt is { } prompt)
            builder.WithPrompt(prompt);

        return builder.Build();
    }

    private void ThrowIfDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);
#endif
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _cts?.Cancel();
        _audioBuffer?.Dispose();
        _cts?.Dispose();
        _factory?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (IsListening)
            await StopAsync(CancellationToken.None);

        Dispose();
    }
}
