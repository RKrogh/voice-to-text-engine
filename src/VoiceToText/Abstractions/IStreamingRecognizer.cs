using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceToText.Models;

namespace VoiceToText.Abstractions;

/// <summary>
/// Processes audio in real-time chunks, emitting partial and final results via events.
/// Used for push-to-talk and continuous microphone streaming.
/// </summary>
public interface IStreamingRecognizer : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Raised when a partial (interim) transcription is available.
    /// These results may change as more audio arrives.
    /// </summary>
    event EventHandler<StreamingRecognitionEventArgs>? PartialResultReceived;

    /// <summary>
    /// Raised when a final (stable) transcription segment is available.
    /// </summary>
    event EventHandler<StreamingRecognitionEventArgs>? FinalResultReceived;

    /// <summary>
    /// Start a streaming recognition session.
    /// </summary>
    Task StartAsync(RecognizerOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Feed a chunk of 16-bit PCM audio data (16kHz mono) into the recognizer.
    /// </summary>
    void PushAudio(ReadOnlySpan<byte> pcmData);

    /// <summary>
    /// Feed float samples (range -1.0 to 1.0, 16kHz mono) into the recognizer.
    /// </summary>
    void PushAudio(ReadOnlySpan<float> samples);

    /// <summary>
    /// Signal that no more audio will be sent. Flushes any pending results.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a streaming session is currently active.
    /// </summary>
    bool IsListening { get; }
}
