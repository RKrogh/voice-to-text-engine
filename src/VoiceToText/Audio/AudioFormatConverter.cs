using System;

namespace VoiceToText.Audio;

/// <summary>
/// Utility methods for audio format conversion.
/// Converts between sample rates, channel counts, and PCM/float formats.
/// </summary>
public static class AudioFormatConverter
{
    /// <summary>
    /// Convert stereo 16-bit PCM data to mono by averaging both channels.
    /// </summary>
    /// <param name="stereoData">Raw stereo PCM bytes (16-bit, 2 channels).</param>
    /// <returns>Mono PCM bytes (16-bit, 1 channel).</returns>
    public static byte[] StereoToMono(ReadOnlySpan<byte> stereoData)
    {
        int sampleCount = stereoData.Length / 4; // 2 bytes per sample * 2 channels
        var mono = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            int offset = i * 4;
            short left = (short)(stereoData[offset] | (stereoData[offset + 1] << 8));
            short right = (short)(stereoData[offset + 2] | (stereoData[offset + 3] << 8));
            short mixed = (short)((left + right) / 2);

            mono[i * 2] = (byte)(mixed & 0xFF);
            mono[i * 2 + 1] = (byte)((mixed >> 8) & 0xFF);
        }

        return mono;
    }

    /// <summary>
    /// Resample 16-bit mono PCM audio using linear interpolation.
    /// </summary>
    /// <param name="data">Source PCM bytes (16-bit mono).</param>
    /// <param name="sourceSampleRate">Original sample rate in Hz.</param>
    /// <param name="targetSampleRate">Desired sample rate in Hz.</param>
    /// <returns>Resampled PCM bytes.</returns>
    public static byte[] Resample(ReadOnlySpan<byte> data, int sourceSampleRate, int targetSampleRate)
    {
        if (sourceSampleRate == targetSampleRate)
            return data.ToArray();

        int sourceSamples = data.Length / 2;
        double ratio = (double)sourceSampleRate / targetSampleRate;
        int targetSamples = (int)(sourceSamples / ratio);
        var result = new byte[targetSamples * 2];

        for (int i = 0; i < targetSamples; i++)
        {
            double srcIndex = i * ratio;
            int idx = (int)srcIndex;
            double frac = srcIndex - idx;

            short s0 = ReadSample(data, idx);
            short s1 = (idx + 1 < sourceSamples) ? ReadSample(data, idx + 1) : s0;
            short interpolated = (short)(s0 + (s1 - s0) * frac);

            result[i * 2] = (byte)(interpolated & 0xFF);
            result[i * 2 + 1] = (byte)((interpolated >> 8) & 0xFF);
        }

        return result;
    }

    /// <summary>
    /// Convert 16-bit PCM bytes to float samples in range [-1.0, 1.0].
    /// </summary>
    public static float[] PcmToFloat(ReadOnlySpan<byte> pcm16Data)
    {
        int sampleCount = pcm16Data.Length / 2;
        var floats = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(pcm16Data[i * 2] | (pcm16Data[i * 2 + 1] << 8));
            floats[i] = sample / 32768f;
        }

        return floats;
    }

    /// <summary>
    /// Convert float samples in range [-1.0, 1.0] to 16-bit PCM bytes.
    /// </summary>
    public static byte[] FloatToPcm(ReadOnlySpan<float> floatData)
    {
        var pcm = new byte[floatData.Length * 2];

        for (int i = 0; i < floatData.Length; i++)
        {
            float clamped = Math.Clamp(floatData[i], -1f, 1f);
            short sample = (short)(clamped * 32767f);
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return pcm;
    }

    private static short ReadSample(ReadOnlySpan<byte> data, int sampleIndex)
    {
        int offset = sampleIndex * 2;
        return (short)(data[offset] | (data[offset + 1] << 8));
    }
}
