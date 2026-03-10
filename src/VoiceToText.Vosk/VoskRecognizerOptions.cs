namespace VoiceToText.Vosk;

/// <summary>
/// Configuration options for the Vosk speech recognizer.
/// </summary>
public class VoskRecognizerOptions
{
    /// <summary>
    /// Path to the Vosk model directory (e.g. "models/vosk-model-small-en-us-0.15").
    /// Download models from https://alphacephei.com/vosk/models
    /// </summary>
    public required string ModelPath { get; set; }

    /// <summary>
    /// Maximum number of alternative results to return. 0 = only best result.
    /// </summary>
    public int MaxAlternatives { get; set; }

    /// <summary>
    /// Enable word-level timestamps in results. Default: false.
    /// </summary>
    public bool Words { get; set; }
}
