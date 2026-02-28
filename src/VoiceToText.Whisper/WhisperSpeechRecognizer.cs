using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceToText.Abstractions;
using VoiceToText.Audio;
using VoiceToText.Models;
using Whisper.net;

namespace VoiceToText.Whisper;

/// <summary>
/// Batch speech recognizer powered by Whisper.net.
/// Transcribes complete audio streams (WAV or raw 16kHz mono 16-bit PCM).
/// </summary>
public sealed class WhisperSpeechRecognizer : ISpeechRecognizer
{
    private readonly WhisperRecognizerOptions _options;
    private readonly ILogger<WhisperSpeechRecognizer> _logger;
    private WhisperFactory? _factory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WhisperSpeechRecognizer"/>.
    /// </summary>
    public WhisperSpeechRecognizer(
        IOptions<WhisperRecognizerOptions> options,
        ILogger<WhisperSpeechRecognizer> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        RecognizerOptions? options,
        CancellationToken cancellationToken
    )
    {
        ThrowIfDisposed();

        var segments = new List<TranscriptionSegment>();

        await foreach (
            var segment in TranscribeSegmentsAsync(audioStream, options, cancellationToken)
        )
        {
            segments.Add(segment);
        }

        var text = string.Join(" ", segments.Select(s => s.Text.Trim()));
        var duration = segments.Count > 0 ? segments[^1].End : TimeSpan.Zero;

        return new TranscriptionResult
        {
            Text = text,
            Segments = segments,
            Duration = duration,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TranscriptionSegment> TranscribeSegmentsAsync(
        Stream audioStream,
        RecognizerOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        ThrowIfDisposed();

        var factory = GetOrCreateFactory();
        using var processor = CreateProcessor(factory, options);

        _logger.LogDebug("Starting Whisper transcription");

        var samples = await PrepareAudioAsync(audioStream, cancellationToken);

        await foreach (var segment in processor.ProcessAsync(samples, cancellationToken))
        {
            _logger.LogTrace(
                "Segment [{Start} -> {End}]: {Text}",
                segment.Start,
                segment.End,
                segment.Text
            );

            yield return new TranscriptionSegment
            {
                Text = segment.Text,
                Start = segment.Start,
                End = segment.End,
                Confidence = 1f - segment.NoSpeechProbability,
            };
        }

        _logger.LogDebug("Whisper transcription complete");
    }

    private async Task<float[]> PrepareAudioAsync(
        Stream audioStream,
        CancellationToken cancellationToken
    )
    {
        var wav = await WavReader.ReadAsync(audioStream, cancellationToken);
        var pcm = wav.PcmData;

        _logger.LogDebug(
            "WAV format: {SampleRate}Hz, {Channels}ch, {Bits}bit",
            wav.SampleRate,
            wav.Channels,
            wav.BitsPerSample
        );

        // Convert to mono if stereo
        if (wav.Channels == 2)
        {
            _logger.LogDebug("Converting stereo to mono");
            pcm = AudioFormatConverter.StereoToMono(pcm);
        }

        // Resample to 16kHz if needed
        if (wav.SampleRate != AudioConstants.DefaultSampleRate)
        {
            _logger.LogDebug(
                "Resampling from {Source}Hz to {Target}Hz",
                wav.SampleRate,
                AudioConstants.DefaultSampleRate
            );
            pcm = AudioFormatConverter.Resample(
                pcm,
                wav.SampleRate,
                AudioConstants.DefaultSampleRate
            );
        }

        // Convert to float samples for Whisper
        return AudioFormatConverter.PcmToFloat(pcm);
    }

    private WhisperFactory GetOrCreateFactory()
    {
        if (_factory is not null)
            return _factory;

        _logger.LogInformation("Loading Whisper model from {ModelPath}", _options.ModelPath);
        _factory = WhisperFactory.FromPath(_options.ModelPath);
        return _factory;
    }

    private WhisperProcessor CreateProcessor(WhisperFactory factory, RecognizerOptions? options)
    {
        var builder = factory.CreateBuilder();

        var language = options?.Language ?? "auto";
        if (language == "auto")
            builder.WithLanguageDetection();
        else
            builder.WithLanguage(language);

        if (_options.Threads > 0)
            builder.WithThreads(_options.Threads);

        if (_options.Translate)
            builder.WithTranslate();

        if (options?.Prompt is { } prompt)
            builder.WithPrompt(prompt);

        if (options?.WordTimestamps is true)
            builder.WithTokenTimestamps();

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
        _factory?.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
