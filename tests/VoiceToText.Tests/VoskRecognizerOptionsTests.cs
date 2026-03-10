using VoiceToText.Vosk;
using Xunit;

namespace VoiceToText.Tests;

public class VoskRecognizerOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new VoskRecognizerOptions { ModelPath = "test-model" };

        Assert.Equal(0, options.MaxAlternatives);
        Assert.False(options.Words);
    }

    [Fact]
    public void ModelPath_IsRequired()
    {
        var options = new VoskRecognizerOptions { ModelPath = "models/vosk-model-small-en-us" };
        Assert.Equal("models/vosk-model-small-en-us", options.ModelPath);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new VoskRecognizerOptions
        {
            ModelPath = "models/vosk-model",
            MaxAlternatives = 3,
            Words = true,
        };

        Assert.Equal("models/vosk-model", options.ModelPath);
        Assert.Equal(3, options.MaxAlternatives);
        Assert.True(options.Words);
    }
}
