using Microsoft.Extensions.DependencyInjection;
using VoiceToText.Abstractions;
using VoiceToText.DependencyInjection;
using VoiceToText.Whisper;
using Xunit;

namespace VoiceToText.Tests;

public class WhisperDependencyInjectionTests
{
    [Fact]
    public void AddWhisperRecognizer_RegistersSpeechRecognizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoiceToText().AddWhisperRecognizer(opts => opts.ModelPath = "test-model.bin");

        var provider = services.BuildServiceProvider();
        var recognizer = provider.GetService<ISpeechRecognizer>();

        Assert.NotNull(recognizer);
        Assert.IsType<WhisperSpeechRecognizer>(recognizer);
    }

    [Fact]
    public void AddWhisperRecognizer_RegistersStreamingRecognizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoiceToText().AddWhisperRecognizer(opts => opts.ModelPath = "test-model.bin");

        var provider = services.BuildServiceProvider();
        var recognizer = provider.GetService<IStreamingRecognizer>();

        Assert.NotNull(recognizer);
        Assert.IsType<WhisperStreamingRecognizer>(recognizer);
    }

    [Fact]
    public void AddWhisperRecognizer_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddVoiceToText()
            .AddWhisperRecognizer(opts =>
            {
                opts.ModelPath = "models/ggml-base.bin";
                opts.Threads = 4;
                opts.Translate = true;
                opts.StreamingBufferDuration = TimeSpan.FromSeconds(5);
            });

        var provider = services.BuildServiceProvider();
        var options =
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<WhisperRecognizerOptions>>();

        Assert.Equal("models/ggml-base.bin", options.Value.ModelPath);
        Assert.Equal(4, options.Value.Threads);
        Assert.True(options.Value.Translate);
        Assert.Equal(TimeSpan.FromSeconds(5), options.Value.StreamingBufferDuration);
    }
}
