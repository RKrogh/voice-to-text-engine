using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceToText.Models;

namespace VoiceToText.Abstractions;

/// <summary>
/// Represents a source of audio data (microphone, file, network stream, etc.).
/// Audio is delivered as 16kHz mono 16-bit PCM via the <see cref="DataAvailable"/> event.
/// </summary>
public interface IAudioSource : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Raised when a new chunk of audio data is available.
    /// </summary>
    event EventHandler<AudioDataEventArgs>? DataAvailable;

    /// <summary>
    /// Raised when the audio source has stopped producing data.
    /// </summary>
    event EventHandler? Stopped;

    /// <summary>
    /// Start capturing or reading audio.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop capturing or reading audio.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this source is currently producing audio.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// The audio format delivered by this source.
    /// </summary>
    AudioFormat Format { get; }
}
