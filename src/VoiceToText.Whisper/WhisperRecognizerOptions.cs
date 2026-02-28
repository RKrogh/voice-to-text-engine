namespace VoiceToText.Whisper;

/// <summary>
/// Configuration options for the Whisper.net speech recognizer.
/// </summary>
public class WhisperRecognizerOptions
{
    /// <summary>
    /// Path to the Whisper GGML model file (e.g. "ggml-base.bin").
    /// </summary>
    public required string ModelPath { get; set; }

    /// <summary>
    /// Number of threads to use for processing. 0 = auto (use all available cores).
    /// </summary>
    public int Threads { get; set; }

    /// <summary>
    /// Enable translation to English. When true, non-English audio is translated.
    /// </summary>
    public bool Translate { get; set; }

    /// <summary>
    /// Buffer duration for simulated streaming. Whisper is a batch model, so streaming
    /// is simulated by accumulating audio and re-processing periodically.
    /// Default: 3 seconds.
    /// </summary>
    public TimeSpan StreamingBufferDuration { get; set; } = TimeSpan.FromSeconds(3);
}
