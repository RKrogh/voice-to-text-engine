# VoiceToText

[![Build](https://github.com/RKrogh/voice-to-text-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/RKrogh/voice-to-text-engine/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/VoiceToText.svg)](https://www.nuget.org/packages/VoiceToText)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A C# speech-to-text library with a provider abstraction layer. Supports multiple STT backends through a common interface, designed for reuse across transcribers, Unity games, desktop apps, and more.

## Packages

| Package | Description |
|---|---|
| `VoiceToText` | Core abstractions (zero STT dependencies) |
| `VoiceToText.Vosk` | Vosk provider — true streaming, lightweight, sub-second latency |
| `VoiceToText.Whisper` | Whisper.net provider — best accuracy, batch model (streaming simulated with 2-3s buffer) |
| `VoiceToText.Audio.NAudio` | Windows microphone capture via NAudio |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.100 or later)
- A speech model — Whisper models auto-download; Vosk models must be [downloaded manually](https://alphacephei.com/vosk/models)

## Usage

### Register services

```csharp
services.AddVoiceToText()
    .AddVoskRecognizer(opts => opts.ModelPath = "models/vosk-model-small-en-us")
    .AddNAudioMicrophone();
```

### Batch transcription

```csharp
await using var recognizer = serviceProvider.GetRequiredService<ISpeechRecognizer>();
await using var stream = File.OpenRead("audio.wav");
var result = await recognizer.TranscribeAsync(stream);
Console.WriteLine(result.Text);
```

### Real-time streaming

```csharp
var streaming = serviceProvider.GetRequiredService<IStreamingRecognizer>();
streaming.FinalResultReceived += (_, e) => Console.WriteLine(e.Text);
await streaming.StartAsync();
// Push audio chunks...
await streaming.StopAsync();
```

## Console Sample

The included console sample supports file transcription and live microphone input.

```bash
# Transcribe a WAV file (defaults to Whisper)
dotnet run --project samples/VoiceToText.Samples.Console -- hello-world.wav

# Live microphone with Vosk
dotnet run --project samples/VoiceToText.Samples.Console -- --mic --vosk --model vosk-model-small-en-us-0.15

# Live microphone with Whisper (default)
dotnet run --project samples/VoiceToText.Samples.Console -- --mic
```

**Options:**

| Flag | Description |
|---|---|
| `--mic` | Live microphone streaming (Windows only) |
| `--vosk` | Use Vosk provider |
| `--whisper` | Use Whisper provider (default) |
| `--model <path>` | Path to model file (.bin) or directory |

## Project Structure

```
src/
  VoiceToText/                    Core abstractions (zero STT dependencies)
  VoiceToText.Vosk/               Vosk provider (true streaming)
  VoiceToText.Whisper/            Whisper.net provider (batch, best accuracy)
  VoiceToText.Audio.NAudio/       Windows microphone capture
samples/
  VoiceToText.Samples.Console/    Push-to-talk console demo
tests/
  VoiceToText.Tests/              Unit tests (26 tests)
```

## Architecture

### Core Interfaces

- **`ISpeechRecognizer`** — batch/file transcription (`TranscribeAsync`, `TranscribeSegmentsAsync`)
- **`IStreamingRecognizer`** — real-time streaming with `PushAudio()` + `PartialResultReceived`/`FinalResultReceived` events
- **`IAudioSource`** — audio input abstraction (microphone, file) with `DataAvailable` event

### Audio Pipeline

All audio is normalized to **16kHz mono 16-bit PCM**. The `AudioFormatConverter` utility handles stereo-to-mono, resampling, and PCM/float conversion.

## Target Frameworks

- `net10.0` — primary target
- `netstandard2.1` — Unity 2021+ and broad compatibility

## Building

```bash
dotnet build
dotnet test
dotnet pack    # produces 4 .nupkg files
```

## Contributing

Feature branches off `main`. PRs welcome.

## License

[MIT](LICENSE)
