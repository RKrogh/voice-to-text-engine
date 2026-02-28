# VoiceToText

A C# speech-to-text library with a provider abstraction layer. Supports multiple STT backends through a common interface, designed for reuse across transcribers, Unity games, desktop apps, and more.

## Status

**Early development** — the core abstractions and audio utilities are implemented. Provider implementations (Vosk, Whisper.net) and the audio capture layer (NAudio) are scaffolded but not yet implemented.

What's done:
- Core interfaces: `ISpeechRecognizer`, `IStreamingRecognizer`, `IAudioSource`
- Data models: `TranscriptionResult`, `TranscriptionSegment`, `AudioFormat`, etc.
- Audio utilities: format conversion, resampling, stereo-to-mono, PCM/float conversion
- Dependency injection entry point (`AddVoiceToText()`)
- Solution structure and build configuration

What's not yet implemented:
- Vosk provider (real-time streaming)
- Whisper.net provider (batch transcription)
- NAudio microphone capture (Windows)
- Console sample app
- Unit tests

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.100 or later)

## Building

```bash
dotnet build
```

To build just the core library:

```bash
dotnet build src/VoiceToText/
```

## Project Structure

```
src/
  VoiceToText/                    Core abstractions (zero STT dependencies)
  VoiceToText.Vosk/               Vosk provider (not yet implemented)
  VoiceToText.Whisper/            Whisper.net provider (not yet implemented)
  VoiceToText.Audio.NAudio/       Windows microphone capture (not yet implemented)
samples/
  VoiceToText.Samples.Console/    Push-to-talk demo (not yet implemented)
tests/
  VoiceToText.Tests/              Unit tests (not yet implemented)
```

## Architecture

### Core Interfaces

**`ISpeechRecognizer`** — batch/file transcription:

```csharp
Task<TranscriptionResult> TranscribeAsync(Stream audioStream, ...);
IAsyncEnumerable<TranscriptionSegment> TranscribeSegmentsAsync(Stream audioStream, ...);
```

**`IStreamingRecognizer`** — real-time audio with partial/final results via events:

```csharp
Task StartAsync(...);
void PushAudio(ReadOnlySpan<byte> pcmData);
void PushAudio(ReadOnlySpan<float> samples);
Task StopAsync(...);
event EventHandler<StreamingRecognitionEventArgs>? PartialResultReceived;
event EventHandler<StreamingRecognitionEventArgs>? FinalResultReceived;
```

**`IAudioSource`** — audio input (microphone, file, etc.):

```csharp
Task StartAsync(...);
Task StopAsync(...);
event EventHandler<AudioDataEventArgs>? DataAvailable;
```

### Intended Usage (once providers are implemented)

```csharp
// Register services
services.AddVoiceToText()
    .AddVoskRecognizer(opts => opts.ModelPath = "models/vosk-model-small-en-us")
    .AddNAudioMicrophone();

// Batch transcription
await using var recognizer = serviceProvider.GetRequiredService<ISpeechRecognizer>();
await using var stream = File.OpenRead("audio.wav");
var result = await recognizer.TranscribeAsync(stream);
Console.WriteLine(result.Text);

// Real-time streaming
var streaming = serviceProvider.GetRequiredService<IStreamingRecognizer>();
streaming.FinalResultReceived += (_, e) => Console.WriteLine(e.Text);
await streaming.StartAsync();
// Push audio chunks...
await streaming.StopAsync();
```

### Audio Pipeline

All audio is normalized to **16kHz mono 16-bit PCM**. The `AudioFormatConverter` utility handles:
- Stereo to mono conversion
- Sample rate resampling
- PCM ↔ float conversion

### Providers

| Provider | Package | License | Characteristics |
|---|---|---|---|
| Vosk | `VoiceToText.Vosk` | Apache 2.0 | True streaming, lightweight, sub-second latency |
| Whisper.net | `VoiceToText.Whisper` | MIT | Best accuracy, batch model (streaming is simulated with 2-3s latency buffer) |

### Audio Sources

| Source | Package | Platform |
|---|---|---|
| NAudio | `VoiceToText.Audio.NAudio` | Windows only |

## Target Frameworks

- `net10.0` — primary target
- `netstandard2.1` — Unity 2021+ and broad compatibility

## Contributing

Feature branches off `main`. Providers are developed in parallel:

```
main → feature/core-abstractions
     → feature/vosk-provider
     → feature/whisper-provider
     → feature/audio-naudio
     → feature/sample-console
```

## License

[MIT](LICENSE)
