using Microsoft.Extensions.Options;
using NAudio.Wave;
using VoiceToText.Abstractions;
using VoiceToText.Audio;
using VoiceToText.Models;

namespace VoiceToText.Audio.NAudio;

/// <summary>
/// Captures audio from a Windows microphone using NAudio's <see cref="WaveInEvent"/>.
/// Delivers 16kHz mono 16-bit PCM via the <see cref="IAudioSource.DataAvailable"/> event.
/// </summary>
public sealed class NAudioMicrophoneSource : IAudioSource
{
    private readonly NAudioOptions _options;
    private WaveInEvent? _waveIn;
    private TaskCompletionSource? _stoppedTcs;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<AudioDataEventArgs>? DataAvailable;

    /// <inheritdoc />
    public event EventHandler? Stopped;

    /// <inheritdoc />
    public bool IsCapturing { get; private set; }

    /// <inheritdoc />
    public AudioFormat Format { get; } = AudioFormat.Default;

    /// <summary>
    /// Initializes a new instance of <see cref="NAudioMicrophoneSource"/>.
    /// </summary>
    public NAudioMicrophoneSource(IOptions<NAudioOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsCapturing)
            return Task.CompletedTask;

        _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _waveIn = new WaveInEvent
        {
            DeviceNumber = _options.DeviceNumber,
            BufferMilliseconds = _options.BufferMilliseconds,
            NumberOfBuffers = _options.NumberOfBuffers,
            WaveFormat = new WaveFormat(
                AudioConstants.DefaultSampleRate,
                AudioConstants.DefaultBitsPerSample,
                AudioConstants.DefaultChannels
            ),
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        IsCapturing = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsCapturing || _waveIn is null)
            return;

        _waveIn.StopRecording();

        // Wait for RecordingStopped to fire so the native recording thread
        // has fully stopped before we allow dispose.
        if (_stoppedTcs is not null)
            await _stoppedTcs.Task.WaitAsync(cancellationToken);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded <= 0)
            return;

        var buffer = new byte[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);

        DataAvailable?.Invoke(this, new AudioDataEventArgs { Buffer = buffer, Format = Format });
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
        CleanupWaveIn();
        _stoppedTcs?.TrySetResult();
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    private void CleanupWaveIn()
    {
        if (_waveIn is null)
            return;

        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.RecordingStopped -= OnRecordingStopped;
        _waveIn.Dispose();
        _waveIn = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CleanupWaveIn();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
