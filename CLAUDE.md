# VoiceToText - Claude Code Project Guide

## Project Overview

C# open-source speech-to-text engine with a provider abstraction layer. Supports multiple STT backends (Whisper.net, Vosk) through a common interface. Designed for reuse across contexts: transcribers, Unity games, desktop apps, etc.

## Architecture

### Solution Structure

- `src/VoiceToText/` - Core abstractions library (zero STT deps). Interfaces, models, audio utilities.
- `src/VoiceToText.Whisper/` - Whisper.net provider (MIT, best accuracy, batch model)
- `src/VoiceToText.Vosk/` - Vosk provider (Apache 2.0, true streaming, lightweight)
- `src/VoiceToText.Audio.NAudio/` - Windows microphone capture via NAudio
- `samples/VoiceToText.Samples.Console/` - Push-to-talk console demo
- `tests/VoiceToText.Tests/` - Unit tests

### Key Interfaces (in `VoiceToText.Abstractions`)

- `ISpeechRecognizer` - Batch/file transcription (`TranscribeAsync`, `TranscribeSegmentsAsync`)
- `IStreamingRecognizer` - Real-time streaming with `PushAudio()` + events (`PartialResultReceived`, `FinalResultReceived`)
- `IAudioSource` - Audio input abstraction (microphone, file) with `DataAvailable` event

### Provider Pattern

Standard .NET DI via extension methods:
```csharp
services.AddVoiceToText()
    .AddVoskRecognizer(opts => opts.ModelPath = "models/vosk-model-small-en-us")
    .AddNAudioMicrophone();
```

### Audio Pipeline

All audio normalized to 16kHz mono 16-bit PCM. `AudioFormatConverter` handles resampling, stereo-to-mono, and PCM/float conversion.

## Build & Run

```bash
dotnet build                              # Build all projects
dotnet build src/VoiceToText/             # Build core only
dotnet test                               # Run tests
dotnet run --project samples/VoiceToText.Samples.Console  # Run sample
```

## Target Frameworks

- `net10.0` - Primary target (.NET 10)
- `netstandard2.1` - Unity 2021+ and broad compatibility
- Polyfills in `src/VoiceToText/Polyfills.cs` for `init`, `required` keywords on netstandard2.1

## Key Design Decisions

- Whisper streaming is **simulated** (2-3s latency buffer) since Whisper is batch-only. Vosk has true sub-second streaming.
- Push-to-talk is an orchestration pattern (consumer calls Start/Stop), not a built-in component.
- NAudio is Windows-only, separated into its own package. Cross-platform audio (PortAudio) can be added later.
- Central package management via `Directory.Packages.props`.

## Git Workflow

Feature branches off `main`. Providers developed in parallel:
- `feature/core-abstractions` → `feature/vosk-provider`, `feature/whisper-provider`, `feature/audio-naudio` (parallel) → `feature/sample-console`

## Dependencies

| Package | Version | Used In |
|---|---|---|
| Whisper.net | 1.9.0 | VoiceToText.Whisper |
| Vosk | 0.3.38 | VoiceToText.Vosk |
| NAudio | 2.2.1 | VoiceToText.Audio.NAudio |
| MS.Extensions.DI.Abstractions | 9.0.0 | VoiceToText (core) |
| MS.Extensions.Options | 9.0.0 | VoiceToText (core) |
| MS.Extensions.Logging.Abstractions | 9.0.0 | VoiceToText (core) |

## Conventions

- MIT license
- Nullable reference types enabled
- Warnings as errors
- XML documentation on all public APIs
- `LangVersion=preview` for latest C# features
