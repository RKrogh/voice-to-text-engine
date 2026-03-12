using System;

namespace VoiceToText.Models;

/// <summary>
/// Event data containing a chunk of audio from an <see cref="Abstractions.IAudioSource"/>.
/// </summary>
public sealed class AudioDataEventArgs : EventArgs
{
    /// <summary>Raw audio bytes in the format specified by <see cref="Format"/>.</summary>
    public required ReadOnlyMemory<byte> Buffer { get; init; }

    /// <summary>The audio format of the data in <see cref="Buffer"/>.</summary>
    public required AudioFormat Format { get; init; }
}
