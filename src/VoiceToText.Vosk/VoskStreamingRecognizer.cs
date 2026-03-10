using System.Text.Json;
using global::Vosk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceToText.Abstractions;
using VoiceToText.Audio;
using VoiceToText.Models;

namespace VoiceToText.Vosk;

/// <summary>
/// True streaming speech recognizer powered by Vosk.
/// Vosk natively supports real-time streaming with sub-second latency —
/// audio chunks are fed directly to the recognizer without buffering.
/// </summary>
public sealed class VoskStreamingRecognizer : IStreamingRecognizer
{
    private readonly VoskRecognizerOptions _options;
    private readonly ILogger<VoskStreamingRecognizer> _logger;
    private Model? _model;

    private VoskRecognizer? _recognizer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="VoskStreamingRecognizer"/>.
    /// </summary>
    public VoskStreamingRecognizer(
        IOptions<VoskRecognizerOptions> options,
        ILogger<VoskStreamingRecognizer> logger
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

        var model = GetOrCreateModel();
        _recognizer = new VoskRecognizer(model, AudioConstants.DefaultSampleRate);

        if (_options.Words || options?.WordTimestamps is true)
            _recognizer.SetWords(true);

        if (_options.MaxAlternatives > 0)
            _recognizer.SetMaxAlternatives(_options.MaxAlternatives);

        IsListening = true;

        _logger.LogDebug("Vosk streaming session started");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void PushAudio(ReadOnlySpan<byte> pcmData)
    {
        ThrowIfDisposed();

        if (!IsListening || _recognizer is null)
            throw new InvalidOperationException(
                "No active streaming session. Call StartAsync first."
            );

        // Vosk accepts byte[] — copy from span
        var buffer = pcmData.ToArray();

        if (_recognizer.AcceptWaveform(buffer, buffer.Length))
        {
            var text = ExtractText(_recognizer.Result());
            if (!string.IsNullOrWhiteSpace(text))
            {
                FinalResultReceived?.Invoke(
                    this,
                    new StreamingRecognitionEventArgs { Text = text, IsFinal = true }
                );
            }
        }
        else
        {
            var text = ExtractPartialText(_recognizer.PartialResult());
            if (!string.IsNullOrWhiteSpace(text))
            {
                PartialResultReceived?.Invoke(
                    this,
                    new StreamingRecognitionEventArgs { Text = text, IsFinal = false }
                );
            }
        }
    }

    /// <inheritdoc />
    public void PushAudio(ReadOnlySpan<float> samples)
    {
        var pcm = AudioFormatConverter.FloatToPcm(samples);
        PushAudio(pcm.AsSpan());
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsListening)
            return Task.CompletedTask;

        IsListening = false;

        // Flush remaining audio
        if (_recognizer is not null)
        {
            var text = ExtractText(_recognizer.FinalResult());
            if (!string.IsNullOrWhiteSpace(text))
            {
                FinalResultReceived?.Invoke(
                    this,
                    new StreamingRecognitionEventArgs { Text = text, IsFinal = true }
                );
            }

            _recognizer.Dispose();
            _recognizer = null;
        }

        _logger.LogDebug("Vosk streaming session stopped");

        return Task.CompletedTask;
    }

    private static string? ExtractText(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("text", out var textProp)
            ? textProp.GetString()?.Trim()
            : null;
    }

    private static string? ExtractPartialText(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("partial", out var partialProp)
            ? partialProp.GetString()?.Trim()
            : null;
    }

    private Model GetOrCreateModel()
    {
        if (_model is not null)
            return _model;

        _logger.LogInformation("Loading Vosk model from {ModelPath}", _options.ModelPath);
        global::Vosk.Vosk.SetLogLevel(0);
        _model = new Model(_options.ModelPath);
        return _model;
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
        _recognizer?.Dispose();
        _model?.Dispose();
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
