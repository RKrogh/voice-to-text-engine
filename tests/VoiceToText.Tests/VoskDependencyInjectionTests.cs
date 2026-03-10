using Microsoft.Extensions.DependencyInjection;
using VoiceToText.Abstractions;
using VoiceToText.DependencyInjection;
using VoiceToText.Vosk;
using Xunit;

namespace VoiceToText.Tests;

public class VoskDependencyInjectionTests
{
    [Fact]
    public void AddVoskRecognizer_RegistersSpeechRecognizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoiceToText().AddVoskRecognizer(opts => opts.ModelPath = "test-model");

        var provider = services.BuildServiceProvider();
        var recognizer = provider.GetService<ISpeechRecognizer>();

        Assert.NotNull(recognizer);
        Assert.IsType<VoskSpeechRecognizer>(recognizer);
    }

    [Fact]
    public void AddVoskRecognizer_RegistersStreamingRecognizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoiceToText().AddVoskRecognizer(opts => opts.ModelPath = "test-model");

        var provider = services.BuildServiceProvider();
        var recognizer = provider.GetService<IStreamingRecognizer>();

        Assert.NotNull(recognizer);
        Assert.IsType<VoskStreamingRecognizer>(recognizer);
    }

    [Fact]
    public void AddVoskRecognizer_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddVoiceToText()
            .AddVoskRecognizer(opts =>
            {
                opts.ModelPath = "models/vosk-model-small-en-us";
                opts.MaxAlternatives = 3;
                opts.Words = true;
            });

        var provider = services.BuildServiceProvider();
        var options =
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<VoskRecognizerOptions>>();

        Assert.Equal("models/vosk-model-small-en-us", options.Value.ModelPath);
        Assert.Equal(3, options.Value.MaxAlternatives);
        Assert.True(options.Value.Words);
    }
}
