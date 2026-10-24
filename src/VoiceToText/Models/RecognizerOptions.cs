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

    /// <summary>
    /// Optional vocabulary to restrict recognition to, as words or short phrases. Providers that
    /// support it (Vosk: a runtime grammar) only return these words, which raises accuracy for
    /// command-style input a lot. Providers without support ignore it. Overrides any provider
    /// default for this operation; an empty list keeps the provider default.
    /// </summary>
    public IReadOnlyList<string>? Phrases { get; set; }
}
