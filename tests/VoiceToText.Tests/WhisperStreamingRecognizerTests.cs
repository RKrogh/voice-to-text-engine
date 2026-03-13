using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoiceToText.Whisper;
using Xunit;

namespace VoiceToText.Tests;

public class WhisperStreamingRecognizerTests
{
    private static WhisperStreamingRecognizer CreateRecognizer(
        string modelPath = "fake-model.bin",
        long maxBufferBytes = 50 * 1024 * 1024
    )
    {
        var options = Options.Create(new WhisperRecognizerOptions
        {
            ModelPath = modelPath,
            MaxStreamingBufferBytes = maxBufferBytes,
        });
        return new WhisperStreamingRecognizer(options, NullLogger<WhisperStreamingRecognizer>.Instance);
    }

    [Fact]
    public void IsListening_DefaultsFalse()
    {
        using var recognizer = CreateRecognizer();
        Assert.False(recognizer.IsListening);
    }

    [Fact]
    public void PushAudio_WithoutStart_Throws()
    {
        using var recognizer = CreateRecognizer();

        Assert.Throws<InvalidOperationException>(() => recognizer.PushAudio(new byte[] { 0, 0 }));
    }

    [Fact]
    public void PushAudioFloat_WithoutStart_Throws()
    {
        using var recognizer = CreateRecognizer();

        Assert.Throws<InvalidOperationException>(() => recognizer.PushAudio(new float[] { 0f }));
    }

    [Fact]
    public async Task StopAsync_WhenNotListening_DoesNotThrow()
    {
        using var recognizer = CreateRecognizer();
        await recognizer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyDisposed_Throws()
    {
        var recognizer = CreateRecognizer();
        recognizer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => recognizer.StartAsync(null, CancellationToken.None)
        );
    }

    [Fact]
    public void PushAudio_WhenDisposed_Throws()
    {
        var recognizer = CreateRecognizer();
        recognizer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => recognizer.PushAudio(new byte[] { 0, 0 }));
    }

    [Fact]
    public async Task PushAudio_ExceedsMaxBufferSize_ThrowsInvalidOperationException()
    {
        // Use a tiny buffer limit to make it easy to exceed
        var recognizer = CreateRecognizer(maxBufferBytes: 100);

        await recognizer.StartAsync(null, CancellationToken.None);

        var ex = Assert.Throws<InvalidOperationException>(
            () => recognizer.PushAudio(new byte[101])
        );

        Assert.Contains("exceed the maximum allowed size", ex.Message);

        // Dispose directly — StopAsync would try to process remaining audio (needs model)
        recognizer.Dispose();
    }

    [Fact]
    public async Task PushAudio_CumulativeExceedsMaxBufferSize_ThrowsInvalidOperationException()
    {
        var recognizer = CreateRecognizer(maxBufferBytes: 100);

        await recognizer.StartAsync(null, CancellationToken.None);

        // First push fits
        recognizer.PushAudio(new byte[60]);

        // Second push pushes it over the limit
        var ex = Assert.Throws<InvalidOperationException>(
            () => recognizer.PushAudio(new byte[60])
        );

        Assert.Contains("exceed the maximum allowed size", ex.Message);

        recognizer.Dispose();
    }

    [Fact]
    public async Task PushAudio_WithinBufferLimit_DoesNotThrow()
    {
        var recognizer = CreateRecognizer(maxBufferBytes: 200);

        await recognizer.StartAsync(null, CancellationToken.None);

        recognizer.PushAudio(new byte[100]);
        recognizer.PushAudio(new byte[50]);
        // Should not throw — 150 < 200

        recognizer.Dispose();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyListening_ThrowsInvalidOperationException()
    {
        var recognizer = CreateRecognizer();

        await recognizer.StartAsync(null, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => recognizer.StartAsync(null, CancellationToken.None)
        );

        recognizer.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var recognizer = CreateRecognizer();

        await recognizer.DisposeAsync();
        await recognizer.DisposeAsync();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var recognizer = CreateRecognizer();

        recognizer.Dispose();
        recognizer.Dispose();
    }
}
