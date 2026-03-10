using Microsoft.Extensions.DependencyInjection;
using VoiceToText.Abstractions;
using VoiceToText.Audio.NAudio;

namespace VoiceToText.DependencyInjection;

/// <summary>
/// Extension methods for registering NAudio microphone services.
/// </summary>
public static class NAudioServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NAudio microphone source as an <see cref="IAudioSource"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="NAudioOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNAudioMicrophone(
        this IServiceCollection services,
        Action<NAudioOptions>? configure = null
    )
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<NAudioOptions>(_ => { });

        services.AddSingleton<IAudioSource, NAudioMicrophoneSource>();
        return services;
    }
}
