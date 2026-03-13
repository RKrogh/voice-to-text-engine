using VoiceToText.Whisper;
using Xunit;

namespace VoiceToText.Tests;

public class WavReaderTests
{
    /// <summary>
    /// Builds a minimal valid WAV byte array with the given PCM data and optional chunk size overrides.
    /// </summary>
    private static byte[] BuildWav(
        byte[] pcmData,
        int? dataChunkSizeOverride = null,
        short audioFormat = 1,
        short channels = 1,
        int sampleRate = 16000,
        short bitsPerSample = 16
    )
    {
        var dataChunkSize = dataChunkSizeOverride ?? pcmData.Length;
        var byteRate = sampleRate * channels * (bitsPerSample / 8);
        var blockAlign = (short)(channels * (bitsPerSample / 8));

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + pcmData.Length); // file size - 8
        writer.Write("WAVE"u8);

        // fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16); // chunk size
        writer.Write(audioFormat);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataChunkSize);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    [Fact]
    public async Task ReadAsync_ValidWav_ReturnsCorrectData()
    {
        var pcm = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var wav = BuildWav(pcm);

        using var stream = new MemoryStream(wav);
        var result = await WavReader.ReadAsync(stream);

        Assert.Equal(16000, result.SampleRate);
        Assert.Equal(1, result.Channels);
        Assert.Equal(16, result.BitsPerSample);
        Assert.Equal(pcm, result.PcmData);
    }

    [Fact]
    public async Task ReadAsync_ChunkSizeExceedsMax_ThrowsInvalidOperationException()
    {
        // Craft a WAV with a data chunk declaring > 1GB size
        var pcm = new byte[] { 0x01, 0x02 };
        var wav = BuildWav(pcm, dataChunkSizeOverride: 1_073_741_825); // 1GB + 1

        using var stream = new MemoryStream(wav);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(stream)
        );

        Assert.Contains("exceeds the maximum allowed size", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_NegativeChunkSize_ThrowsInvalidOperationException()
    {
        var pcm = new byte[] { 0x01, 0x02 };
        var wav = BuildWav(pcm, dataChunkSizeOverride: -1);

        using var stream = new MemoryStream(wav);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(stream)
        );

        Assert.Contains("exceeds the maximum allowed size", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_TruncatedFile_ThrowsDescriptiveException()
    {
        // Valid RIFF + WAVE header, then truncated — fmt chunk says 16 bytes but stream ends
        var wav = new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0x24, 0x00, 0x00, 0x00, // file size
            (byte)'W', (byte)'A', (byte)'V', (byte)'E',
            (byte)'f', (byte)'m', (byte)'t', (byte)' ',
            0x10, 0x00, 0x00, 0x00, // fmt chunk size = 16
            // Truncated here — missing the actual fmt data
        };

        using var stream = new MemoryStream(wav);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(stream)
        );

        Assert.Contains("truncated or corrupted", ex.Message);
        Assert.IsType<EndOfStreamException>(ex.InnerException);
    }

    [Fact]
    public async Task ReadAsync_MissingRiffHeader_ThrowsInvalidOperationException()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B };

        using var stream = new MemoryStream(data);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(stream)
        );

        Assert.Contains("missing RIFF header", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_MissingWaveIdentifier_ThrowsInvalidOperationException()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write("RIFF"u8);
        writer.Write(0);
        writer.Write("NOPE"u8);

        ms.Position = 0;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(ms)
        );

        Assert.Contains("missing WAVE identifier", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_NoDataChunk_ThrowsInvalidOperationException()
    {
        // Valid header with fmt chunk but no data chunk
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8);
        writer.Write(28); // file size
        writer.Write("WAVE"u8);

        // fmt chunk only
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(16000); // sample rate
        writer.Write(32000); // byte rate
        writer.Write((short)2); // block align
        writer.Write((short)16); // bits per sample

        ms.Position = 0;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(ms)
        );

        Assert.Contains("no data chunk", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_NonPcmFormat_ThrowsInvalidOperationException()
    {
        var pcm = new byte[] { 0x01, 0x02 };
        var wav = BuildWav(pcm, audioFormat: 3); // 3 = IEEE float

        using var stream = new MemoryStream(wav);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WavReader.ReadAsync(stream)
        );

        Assert.Contains("Only PCM WAV is supported", ex.Message);
    }

    [Fact]
    public async Task ReadAsync_StereoWav_ReturnsCorrectChannels()
    {
        var pcm = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var wav = BuildWav(pcm, channels: 2, sampleRate: 44100, bitsPerSample: 16);

        using var stream = new MemoryStream(wav);
        var result = await WavReader.ReadAsync(stream);

        Assert.Equal(2, result.Channels);
        Assert.Equal(44100, result.SampleRate);
    }

    [Fact]
    public async Task ReadAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var pcm = new byte[] { 0x01, 0x02 };
        var wav = BuildWav(pcm);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var stream = new MemoryStream(wav);
        // The cancellation check is inside the chunk loop, after reading RIFF+WAVE header
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WavReader.ReadAsync(stream, cts.Token)
        );
    }
}
