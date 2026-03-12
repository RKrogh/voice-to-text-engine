using System;

namespace VoiceToText.Models;

/// <summary>
/// Event data for streaming recognition results.
/// </summary>
public sealed class StreamingRecognitionEventArgs : EventArgs
{
    /// <summary>
    /// The transcribed text. For partial results this may change as more
    /// audio arrives. For final results this text is stable.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// True if this is a final (stable) result, false for partial (interim).
    /// </summary>
    public required bool IsFinal { get; init; }

    /// <summary>
    /// Confidence score from 0.0 to 1.0, if available.
    /// </summary>
    public float? Confidence { get; init; }
}
