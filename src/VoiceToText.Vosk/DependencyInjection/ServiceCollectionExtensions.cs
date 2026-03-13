using Microsoft.Extensions.DependencyInjection;
using VoiceToText.Abstractions;
using VoiceToText.Vosk;

namespace VoiceToText.DependencyInjection;

/// <summary>
/// Extension methods for registering Vosk recognizer services.
/// </summary>
public static class VoskServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Vosk speech recognizer as both <see cref="ISpeechRecognizer"/>
    /// and <see cref="IStreamingRecognizer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="VoskRecognizerOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoskRecognizer(
        this IServiceCollection services,
        Action<VoskRecognizerOptions> configure
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
        services.AddSingleton<ISpeechRecognizer, VoskSpeechRecognizer>();
        services.AddSingleton<IStreamingRecognizer, VoskStreamingRecognizer>();
        return services;
    }
}
