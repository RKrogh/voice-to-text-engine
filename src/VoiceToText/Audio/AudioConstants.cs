namespace VoiceToText.Audio;

/// <summary>
/// Standard audio format constants used across the VoiceToText pipeline.
/// All audio is normalized to these defaults before being fed to STT providers.
/// </summary>
public static class AudioConstants
{
    /// <summary>16 kHz - required by most STT models (Whisper, Vosk).</summary>
    public const int DefaultSampleRate = 16_000;

    /// <summary>Mono audio - single channel.</summary>
    public const int DefaultChannels = 1;

    /// <summary>16-bit PCM samples.</summary>
    public const int DefaultBitsPerSample = 16;

    /// <summary>Bytes per sample (2 for 16-bit).</summary>
    public const int DefaultBytesPerSample = DefaultBitsPerSample / 8;

    /// <summary>Block alignment: channels * bytes per sample.</summary>
    public const int DefaultBlockAlign = DefaultChannels * DefaultBytesPerSample;

    /// <summary>Byte rate: sample rate * block align.</summary>
    public const int DefaultByteRate = DefaultSampleRate * DefaultBlockAlign;
}
