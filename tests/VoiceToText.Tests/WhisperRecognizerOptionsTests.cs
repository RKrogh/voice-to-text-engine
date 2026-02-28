using VoiceToText.Whisper;
using Xunit;

namespace VoiceToText.Tests;

public class WhisperRecognizerOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new WhisperRecognizerOptions { ModelPath = "test.bin" };

        Assert.Equal(0, options.Threads);
        Assert.False(options.Translate);
        Assert.Equal(TimeSpan.FromSeconds(3), options.StreamingBufferDuration);
    }

    [Fact]
    public void ModelPath_IsRequired()
    {
        var options = new WhisperRecognizerOptions { ModelPath = "my-model.bin" };
        Assert.Equal("my-model.bin", options.ModelPath);
    }
}
