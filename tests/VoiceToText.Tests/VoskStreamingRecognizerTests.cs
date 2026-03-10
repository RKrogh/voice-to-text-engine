using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoiceToText.Vosk;
using Xunit;

namespace VoiceToText.Tests;

public class VoskStreamingRecognizerTests
{
    private static VoskStreamingRecognizer CreateRecognizer(string modelPath = "fake-model")
    {
        var options = Options.Create(new VoskRecognizerOptions { ModelPath = modelPath });
        return new VoskStreamingRecognizer(options, NullLogger<VoskStreamingRecognizer>.Instance);
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
}
