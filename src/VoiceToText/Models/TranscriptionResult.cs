using System;
using System.Collections.Generic;

namespace VoiceToText.Models;

/// <summary>
/// The complete result of a speech transcription operation.
/// </summary>
public sealed class TranscriptionResult
{
    /// <summary>The full transcribed text.</summary>
    public required string Text { get; init; }

    /// <summary>Individual segments with timestamps, if available.</summary>
    public IReadOnlyList<TranscriptionSegment> Segments { get; init; } = [];

    /// <summary>Total duration of the processed audio.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Detected language code (e.g. "en", "de"), if available.</summary>
    public string? DetectedLanguage { get; init; }
}
