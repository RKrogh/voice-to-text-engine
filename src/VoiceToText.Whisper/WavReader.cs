using System.Text;

namespace VoiceToText.Whisper;

/// <summary>
/// Minimal WAV header parser. Reads format info and returns the raw PCM data.
/// </summary>
internal static class WavReader
{
    /// <summary>
    /// Reads a WAV stream and returns sample rate, channels, bits per sample, and raw PCM data.
    /// </summary>
    internal static async Task<WavData> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // RIFF header
        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new InvalidOperationException("Not a valid WAV file: missing RIFF header.");

        reader.ReadInt32(); // file size
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
            throw new InvalidOperationException("Not a valid WAV file: missing WAVE identifier.");

        // Find fmt and data chunks
        int sampleRate = 0,
            channels = 0,
            bitsPerSample = 0;
        byte[]? pcmData = null;

        while (stream.Position < stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (chunkId == "fmt ")
            {
                var audioFormat = reader.ReadInt16(); // 1 = PCM
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byte rate
                reader.ReadInt16(); // block align
                bitsPerSample = reader.ReadInt16();

                if (audioFormat != 1)
                    throw new InvalidOperationException(
                        $"Only PCM WAV is supported, got format {audioFormat}."
                    );

                // Skip any extra fmt bytes
                var remaining = chunkSize - 16;
                if (remaining > 0)
                    reader.ReadBytes(remaining);
            }
            else if (chunkId == "data")
            {
                pcmData = reader.ReadBytes(chunkSize);
            }
            else
            {
                // Skip unknown chunk
                reader.ReadBytes(chunkSize);
            }
        }

        if (pcmData is null)
            throw new InvalidOperationException("WAV file has no data chunk.");

        return new WavData(sampleRate, channels, bitsPerSample, pcmData);
    }

    internal readonly record struct WavData(
        int SampleRate,
        int Channels,
        int BitsPerSample,
        byte[] PcmData
    );
}
