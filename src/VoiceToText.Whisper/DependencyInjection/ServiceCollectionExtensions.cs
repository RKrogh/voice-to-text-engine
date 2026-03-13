using Microsoft.Extensions.DependencyInjection;
using VoiceToText.Abstractions;
using VoiceToText.Whisper;

namespace VoiceToText.DependencyInjection;

/// <summary>
/// Extension methods for registering Whisper.net recognizer services.
/// </summary>
public static class WhisperServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Whisper.net speech recognizer as both <see cref="ISpeechRecognizer"/>
    /// and <see cref="IStreamingRecognizer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="WhisperRecognizerOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWhisperRecognizer(
        this IServiceCollection services,
        Action<WhisperRecognizerOptions> configure
    )
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
#else
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
#endif

        services.Configure(configure);
        services.AddSingleton<ISpeechRecognizer, WhisperSpeechRecognizer>();
        services.AddSingleton<IStreamingRecognizer, WhisperStreamingRecognizer>();
        return services;
    }
}
