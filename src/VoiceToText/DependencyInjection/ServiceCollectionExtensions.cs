using Microsoft.Extensions.DependencyInjection;

namespace VoiceToText.DependencyInjection;

/// <summary>
/// Extension methods for registering core VoiceToText services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core VoiceToText services. Call provider-specific methods
    /// (e.g. <c>AddVoskRecognizer</c>, <c>AddWhisperRecognizer</c>) after this.
    /// </summary>
    public static IServiceCollection AddVoiceToText(this IServiceCollection services)
    {
        // Core registrations go here as the library grows.
        // Currently acts as the entry point in the fluent chain.
        return services;
    }
}
