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

    /// <summary>
    /// Default vocabulary for every session: recognition is limited to these words or phrases.
    /// <see cref="VoiceToText.Models.RecognizerOptions.Phrases"/> overrides it per operation.
    /// Null or empty means free vocabulary. Needs a model with a dynamic graph (the "lgraph" and
    /// small models); words missing from the model are dropped with a Vosk warning.
    /// </summary>
    public IReadOnlyList<string>? Grammar { get; set; }

    /// <summary>
    /// When a grammar is set, also allow Vosk's "[unk]" token, so speech outside the grammar comes
    /// back as "[unk]" instead of being forced onto the nearest grammar word. Default: true.
    /// </summary>
    public bool AllowUnknownWords { get; set; } = true;
}
