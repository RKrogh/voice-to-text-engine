using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoiceToText.Whisper;
using Xunit;

namespace VoiceToText.Tests;

public class WhisperSpeechRecognizerTests
{
    private static WhisperSpeechRecognizer CreateRecognizer(string modelPath = "fake-model.bin")
    {
        var options = Options.Create(new WhisperRecognizerOptions { ModelPath = modelPath });
        return new WhisperSpeechRecognizer(options, NullLogger<WhisperSpeechRecognizer>.Instance);
    }

    [Fact]
    public async Task TranscribeAsync_NullStream_ThrowsArgumentNullException()
    {
        using var recognizer = CreateRecognizer();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => recognizer.TranscribeAsync(null!, null, CancellationToken.None)
        );
    }

    [Fact]
    public async Task TranscribeSegmentsAsync_NullStream_ThrowsArgumentNullException()
    {
        using var recognizer = CreateRecognizer();

        // Must enumerate to trigger the check (IAsyncEnumerable is lazy)
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in recognizer.TranscribeSegmentsAsync(null!, null, CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task TranscribeAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var recognizer = CreateRecognizer();
        recognizer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => recognizer.TranscribeAsync(Stream.Null, null, CancellationToken.None)
        );
    }

    [Fact]
    public async Task TranscribeAsync_EmptyModelPath_ThrowsInvalidOperationException()
    {
        using var recognizer = CreateRecognizer(modelPath: "");

        // Provide a minimal valid WAV so it gets past stream reading to model loading
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recognizer.TranscribeAsync(new MemoryStream(new byte[1]), null, CancellationToken.None)
        );

        Assert.Contains("ModelPath must be set", ex.Message);
    }

    [Fact]
    public async Task TranscribeAsync_WhitespaceModelPath_ThrowsInvalidOperationException()
    {
        using var recognizer = CreateRecognizer(modelPath: "   ");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recognizer.TranscribeAsync(new MemoryStream(new byte[1]), null, CancellationToken.None)
        );

        Assert.Contains("ModelPath must be set", ex.Message);
    }

    [Fact]
    public async Task TranscribeAsync_NonExistentModelPath_ThrowsInvalidOperationException()
    {
        using var recognizer = CreateRecognizer(modelPath: "/tmp/definitely-does-not-exist-model.bin");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recognizer.TranscribeAsync(new MemoryStream(new byte[1]), null, CancellationToken.None)
        );

        Assert.Contains("does not exist", ex.Message);
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
