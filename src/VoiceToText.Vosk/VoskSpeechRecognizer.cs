using System.Runtime.CompilerServices;
using System.Text.Json;
using global::Vosk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceToText.Abstractions;
using VoiceToText.Audio;
using VoiceToText.Models;

namespace VoiceToText.Vosk;

/// <summary>
/// Batch speech recognizer powered by Vosk.
/// Transcribes complete audio streams (WAV or raw 16kHz mono 16-bit PCM).
/// </summary>
public sealed class VoskSpeechRecognizer : ISpeechRecognizer
{
    private readonly VoskRecognizerOptions _options;
    private readonly ILogger<VoskSpeechRecognizer> _logger;
    private Model? _model;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="VoskSpeechRecognizer"/>.
    /// </summary>
    public VoskSpeechRecognizer(
        IOptions<VoskRecognizerOptions> options,
        ILogger<VoskSpeechRecognizer> logger
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

        var model = GetOrCreateModel();
        using var recognizer = CreateRecognizer(model, options);

        _logger.LogDebug("Starting Vosk batch transcription");

        var pcmData = await ReadPcmDataAsync(audioStream, cancellationToken);
        var chunkSize = 4096;
        var offset = 0;

        while (offset < pcmData.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesToRead = Math.Min(chunkSize, pcmData.Length - offset);
            var chunk = new byte[bytesToRead];
            Array.Copy(pcmData, offset, chunk, 0, bytesToRead);

            if (recognizer.AcceptWaveform(chunk, bytesToRead))
            {
                var segment = ParseResult(recognizer.Result());
                if (segment is not null)
                    yield return segment;
            }

            offset += bytesToRead;
        }

        // Flush remaining audio
        var finalSegment = ParseResult(recognizer.FinalResult());
        if (finalSegment is not null)
            yield return finalSegment;

        _logger.LogDebug("Vosk batch transcription complete");
    }

    private static async Task<byte[]> ReadPcmDataAsync(
        Stream audioStream,
        CancellationToken cancellationToken
    )
    {
        // If the stream looks like WAV, skip the header to get raw PCM
        using var ms = new MemoryStream();
        await audioStream.CopyToAsync(ms, 81920, cancellationToken);
        var data = ms.ToArray();

        if (
            data.Length > 44
            && data[0] == 'R'
            && data[1] == 'I'
            && data[2] == 'F'
            && data[3] == 'F'
        )
        {
            // Find the data chunk
            var pos = 12;
            while (pos + 8 < data.Length)
            {
                var chunkId = System.Text.Encoding.ASCII.GetString(data, pos, 4);
                var chunkSize = BitConverter.ToInt32(data, pos + 4);

                if (chunkId == "data")
                {
                    var pcm = new byte[chunkSize];
                    Array.Copy(data, pos + 8, pcm, 0, Math.Min(chunkSize, data.Length - pos - 8));
                    return pcm;
                }

                pos += 8 + chunkSize;
            }
        }

        // Assume raw PCM
        return data;
    }

    private static TranscriptionSegment? ParseResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("text", out var textProp))
            return null;

        var text = textProp.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = TimeSpan.Zero;
        var end = TimeSpan.Zero;
        float? confidence = null;

        if (root.TryGetProperty("result", out var resultArray) && resultArray.GetArrayLength() > 0)
        {
            var firstWord = resultArray[0];
            var lastWord = resultArray[resultArray.GetArrayLength() - 1];

            if (firstWord.TryGetProperty("start", out var startProp))
                start = TimeSpan.FromSeconds(startProp.GetDouble());

            if (lastWord.TryGetProperty("end", out var endProp))
                end = TimeSpan.FromSeconds(endProp.GetDouble());

            // Average confidence across words
            var totalConf = 0.0;
            var wordCount = 0;
            foreach (var word in resultArray.EnumerateArray())
            {
                if (word.TryGetProperty("conf", out var confProp))
                {
                    totalConf += confProp.GetDouble();
                    wordCount++;
                }
            }

            if (wordCount > 0)
                confidence = (float)(totalConf / wordCount);
        }

        return new TranscriptionSegment
        {
            Text = text,
            Start = start,
            End = end,
            Confidence = confidence,
        };
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

    private VoskRecognizer CreateRecognizer(Model model, RecognizerOptions? options)
    {
        var recognizer = new VoskRecognizer(model, AudioConstants.DefaultSampleRate);

        if (_options.Words || options?.WordTimestamps is true)
            recognizer.SetWords(true);

        if (_options.MaxAlternatives > 0)
            recognizer.SetMaxAlternatives(_options.MaxAlternatives);

        return recognizer;
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
        _model?.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
