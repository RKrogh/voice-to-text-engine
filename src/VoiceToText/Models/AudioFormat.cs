using VoiceToText.Audio;

namespace VoiceToText.Models;

/// <summary>
/// Describes the format of an audio stream.
/// </summary>
public sealed class AudioFormat
{
    /// <summary>Sample rate in Hz (default: 16000).</summary>
    public int SampleRate { get; init; } = AudioConstants.DefaultSampleRate;

    /// <summary>Number of audio channels (default: 1 for mono).</summary>
    public int Channels { get; init; } = AudioConstants.DefaultChannels;

    /// <summary>Bits per sample (default: 16).</summary>
    public int BitsPerSample { get; init; } = AudioConstants.DefaultBitsPerSample;

    /// <summary>Bytes per single sample.</summary>
    public int BytesPerSample => BitsPerSample / 8;

    /// <summary>Block alignment: channels * bytes per sample.</summary>
    public int BlockAlign => Channels * BytesPerSample;

    /// <summary>Byte rate: sample rate * block align.</summary>
    public int ByteRate => SampleRate * BlockAlign;

    /// <summary>
    /// Returns the standard 16kHz mono 16-bit format used by STT providers.
    /// </summary>
    public static AudioFormat Default => new();
}
