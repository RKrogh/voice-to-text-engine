using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoiceToText.Vosk;
using Xunit;

namespace VoiceToText.Tests;

public class VoskSpeechRecognizerTests
{
    private static VoskSpeechRecognizer CreateRecognizer(string modelPath = "fake-model")
    {
        var options = Options.Create(new VoskRecognizerOptions { ModelPath = modelPath });
        return new VoskSpeechRecognizer(options, NullLogger<VoskSpeechRecognizer>.Instance);
    }

    [Fact]
    public async Task TranscribeAsync_WhenDisposed_Throws()
    {
        var recognizer = CreateRecognizer();
        recognizer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => recognizer.TranscribeAsync(Stream.Null, null, CancellationToken.None)
        );
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
