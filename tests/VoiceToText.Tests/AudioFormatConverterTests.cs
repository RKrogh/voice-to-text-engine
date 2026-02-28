using VoiceToText.Audio;
using Xunit;

namespace VoiceToText.Tests;

public class AudioFormatConverterTests
{
    [Fact]
    public void StereoToMono_ConvertsCorrectly()
    {
        // Two stereo samples: (100, 200) and (300, 400)
        var stereo = new byte[8];
        WriteInt16(stereo, 0, 100); // left
        WriteInt16(stereo, 2, 200); // right
        WriteInt16(stereo, 4, 300); // left
        WriteInt16(stereo, 6, 400); // right

        var mono = AudioFormatConverter.StereoToMono(stereo);

        Assert.Equal(4, mono.Length); // 2 mono samples * 2 bytes
        Assert.Equal(150, ReadInt16(mono, 0)); // (100+200)/2
        Assert.Equal(350, ReadInt16(mono, 2)); // (300+400)/2
    }

    [Fact]
    public void PcmToFloat_ConvertsCorrectly()
    {
        var pcm = new byte[4];
        WriteInt16(pcm, 0, 0); // silence
        WriteInt16(pcm, 2, 16384); // ~0.5

        var floats = AudioFormatConverter.PcmToFloat(pcm);

        Assert.Equal(2, floats.Length);
        Assert.Equal(0f, floats[0], 0.001f);
        Assert.Equal(0.5f, floats[1], 0.01f);
    }

    [Fact]
    public void FloatToPcm_ConvertsCorrectly()
    {
        var floats = new float[] { 0f, 0.5f, -1f, 1f };

        var pcm = AudioFormatConverter.FloatToPcm(floats);

        Assert.Equal(8, pcm.Length);
        Assert.Equal(0, ReadInt16(pcm, 0));
        Assert.True(Math.Abs(ReadInt16(pcm, 2) - 16383) < 2);
        Assert.Equal(-32767, ReadInt16(pcm, 4));
        Assert.Equal(32767, ReadInt16(pcm, 6));
    }

    [Fact]
    public void PcmToFloat_FloatToPcm_RoundTrips()
    {
        var original = new byte[6];
        WriteInt16(original, 0, 1000);
        WriteInt16(original, 2, -5000);
        WriteInt16(original, 4, 32000);

        var floats = AudioFormatConverter.PcmToFloat(original);
        var roundTripped = AudioFormatConverter.FloatToPcm(floats);

        // Allow +-1 due to float precision
        Assert.True(Math.Abs(ReadInt16(roundTripped, 0) - 1000) <= 1);
        Assert.True(Math.Abs(ReadInt16(roundTripped, 2) - (-5000)) <= 1);
        Assert.True(Math.Abs(ReadInt16(roundTripped, 4) - 32000) <= 1);
    }

    [Fact]
    public void Resample_SameRate_ReturnsCopy()
    {
        var data = new byte[] { 1, 2, 3, 4 };

        var result = AudioFormatConverter.Resample(data, 16000, 16000);

        Assert.Equal(data, result);
    }

    [Fact]
    public void Resample_Downsample_ReducesSamples()
    {
        // 4 samples at 32kHz -> ~2 samples at 16kHz
        var data = new byte[8];
        WriteInt16(data, 0, 100);
        WriteInt16(data, 2, 200);
        WriteInt16(data, 4, 300);
        WriteInt16(data, 6, 400);

        var result = AudioFormatConverter.Resample(data, 32000, 16000);

        Assert.True(result.Length < data.Length);
    }

    private static void WriteInt16(byte[] buffer, int offset, short value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static short ReadInt16(byte[] buffer, int offset)
    {
        return (short)(buffer[offset] | (buffer[offset + 1] << 8));
    }
}
