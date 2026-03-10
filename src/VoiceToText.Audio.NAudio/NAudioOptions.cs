namespace VoiceToText.Audio.NAudio;

/// <summary>
/// Configuration options for the NAudio microphone source.
/// </summary>
public class NAudioOptions
{
    /// <summary>
    /// The recording device number. -1 uses the default device.
    /// </summary>
    public int DeviceNumber { get; set; } = -1;

    /// <summary>
    /// Size of each audio buffer in milliseconds.
    /// </summary>
    public int BufferMilliseconds { get; set; } = 100;

    /// <summary>
    /// Number of audio buffers to use.
    /// </summary>
    public int NumberOfBuffers { get; set; } = 3;
}
