using System;

namespace VoiceToText.Models;

/// <summary>
/// A single timed segment of transcribed speech.
/// </summary>
public sealed class TranscriptionSegment
{
    /// <summary>The transcribed text for this segment.</summary>
    public required string Text { get; init; }

    /// <summary>Start time of the segment within the audio.</summary>
    public required TimeSpan Start { get; init; }

    /// <summary>End time of the segment within the audio.</summary>
    public required TimeSpan End { get; init; }

    /// <summary>
    /// Confidence score from 0.0 to 1.0, if the provider supports it.
    /// Null when confidence is not available.
    /// </summary>
    public float? Confidence { get; init; }
}
