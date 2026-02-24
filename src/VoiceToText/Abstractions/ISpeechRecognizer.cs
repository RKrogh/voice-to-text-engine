using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VoiceToText.Models;

namespace VoiceToText.Abstractions;

/// <summary>
/// Transcribes complete audio (files, pre-recorded streams).
/// </summary>
public interface ISpeechRecognizer : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Transcribe an audio stream and return the full result.
    /// The stream should contain WAV or raw PCM audio (16kHz mono 16-bit).
    /// </summary>
    Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        RecognizerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribe an audio stream, yielding segments as they are produced.
    /// Useful for progress reporting on long files.
    /// </summary>
    IAsyncEnumerable<TranscriptionSegment> TranscribeSegmentsAsync(
        Stream audioStream,
        RecognizerOptions? options = null,
        CancellationToken cancellationToken = default);
}
