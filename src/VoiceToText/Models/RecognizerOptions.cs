namespace VoiceToText.Models;

/// <summary>
/// Options passed to a recognizer for a single transcription operation.
/// </summary>
public class RecognizerOptions
{
    /// <summary>
    /// Language code (e.g. "en", "de") or "auto" for automatic detection.
    /// Default: "auto".
    /// </summary>
    public string Language { get; set; } = "auto";

    /// <summary>
    /// Whether to include word-level timestamps where the provider supports it.
    /// </summary>
    public bool WordTimestamps { get; set; }

    /// <summary>
    /// Optional initial prompt or context to guide the recognizer.
    /// </summary>
    public string? Prompt { get; set; }
}
