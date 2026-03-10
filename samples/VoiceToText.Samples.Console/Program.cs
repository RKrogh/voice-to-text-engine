using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceToText.Abstractions;
using VoiceToText.DependencyInjection;
using Whisper.net.Ggml;

var parsed = ParseArgs(args);

if (parsed.ShowHelp)
{
    PrintUsage();
    return;
}

if (parsed.MicMode)
    await RunMicMode(parsed);
else
    await RunFileMode(parsed);

static async Task RunFileMode(ParsedArgs parsed)
{
    var wavPath = parsed.AudioFile ?? Path.Combine(AppContext.BaseDirectory, "hello-world.wav");

    if (!File.Exists(wavPath))
    {
        Console.Error.WriteLine($"Error: WAV file not found: {wavPath}");
        PrintUsage();
        return;
    }

    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    await ConfigureProvider(services, parsed);

    await using var provider = services.BuildServiceProvider();
    await using var recognizer = provider.GetRequiredService<ISpeechRecognizer>();
    await using var audioStream = File.OpenRead(wavPath);

    Console.WriteLine($"Transcribing: {wavPath} (provider: {parsed.Provider})");
    Console.WriteLine();

    var result = await recognizer.TranscribeAsync(audioStream);

    Console.WriteLine($"Text: {result.Text}");
    Console.WriteLine($"Duration: {result.Duration:mm\\:ss\\.fff}");
    Console.WriteLine();

    if (result.Segments.Count > 0)
    {
        Console.WriteLine("Segments:");
        foreach (var segment in result.Segments)
        {
            Console.WriteLine(
                $"  [{segment.Start:mm\\:ss\\.fff} -> {segment.End:mm\\:ss\\.fff}] {segment.Text}"
            );
        }
    }
}

static async Task RunMicMode(ParsedArgs parsed)
{
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    await ConfigureProvider(services, parsed);
    services.AddNAudioMicrophone();

    await using var provider = services.BuildServiceProvider();

    var audioSource = provider.GetRequiredService<IAudioSource>();
    await using var streamingRecognizer = provider.GetRequiredService<IStreamingRecognizer>();

    streamingRecognizer.PartialResultReceived += (_, e) =>
    {
        Console.Write($"\r  [partial] {e.Text}                    ");
    };

    streamingRecognizer.FinalResultReceived += (_, e) =>
    {
        Console.Write("\r");
        Console.WriteLine($"  [final]   {e.Text}");
    };

    audioSource.DataAvailable += (_, e) =>
    {
        streamingRecognizer.PushAudio(e.Buffer.Span);
    };

    await streamingRecognizer.StartAsync();
    await audioSource.StartAsync();

    Console.WriteLine($"Listening with {parsed.Provider}... Press Enter to stop.");
    Console.WriteLine();
    Console.ReadLine();

    await audioSource.StopAsync();
    await streamingRecognizer.StopAsync();

    Console.WriteLine();
    Console.WriteLine("Done.");
}

static async Task ConfigureProvider(ServiceCollection services, ParsedArgs parsed)
{
    services.AddVoiceToText();

    if (parsed.Provider == "vosk")
    {
        var modelPath = parsed.ModelPath ?? "models/vosk-model-small-en-us-0.15";

        if (!Directory.Exists(modelPath))
        {
            Console.Error.WriteLine($"Error: Vosk model directory not found: {modelPath}");
            Console.Error.WriteLine("Download a model from https://alphacephei.com/vosk/models");
            Console.Error.WriteLine("Extract it and pass the directory path with --model <path>");
            Environment.Exit(1);
        }

        services.AddVoskRecognizer(opts => opts.ModelPath = modelPath);
    }
    else
    {
        var modelPath = parsed.ModelPath ?? "models/ggml-tiny.bin";

        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"Model not found at '{modelPath}'. Downloading ggml-tiny model...");
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(
                GgmlType.Tiny
            );
            using var fileWriter = File.OpenWrite(modelPath);
            await modelStream.CopyToAsync(fileWriter);
            Console.WriteLine("Download complete.");
        }

        services.AddWhisperRecognizer(opts => opts.ModelPath = modelPath);
    }
}

static ParsedArgs ParseArgs(string[] args)
{
    var parsed = new ParsedArgs();
    var positional = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--mic":
                parsed.MicMode = true;
                break;
            case "--vosk":
                parsed.Provider = "vosk";
                break;
            case "--whisper":
                parsed.Provider = "whisper";
                break;
            case "--model" when i + 1 < args.Length:
                parsed.ModelPath = args[++i];
                break;
            case "--help"
            or "-h":
                parsed.ShowHelp = true;
                break;
            default:
                positional.Add(args[i]);
                break;
        }
    }

    if (positional.Count > 0 && !parsed.MicMode)
        parsed.AudioFile = positional[0];

    return parsed;
}

static void PrintUsage()
{
    Console.WriteLine("VoiceToText Console Sample");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- [options] [audio.wav]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine(
        "  --mic              Live microphone streaming mode (Windows only, requires NAudio)"
    );
    Console.WriteLine("  --vosk             Use Vosk provider (true streaming, lightweight)");
    Console.WriteLine("  --whisper          Use Whisper provider (default, best accuracy)");
    Console.WriteLine("  --model <path>     Path to model file (Whisper .bin) or directory (Vosk)");
    Console.WriteLine("  -h, --help         Show this help");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -- hello-world.wav");
    Console.WriteLine("  dotnet run -- --mic");
    Console.WriteLine("  dotnet run -- --mic --vosk --model vosk-model-small-en-us-0.15");
    Console.WriteLine("  dotnet run -- --vosk --model vosk-model-small-en-us-0.15 recording.wav");
    Console.WriteLine();
    Console.WriteLine(
        "Whisper models are auto-downloaded if missing. Vosk models must be downloaded"
    );
    Console.WriteLine("manually from https://alphacephei.com/vosk/models");
}

class ParsedArgs
{
    public bool MicMode { get; set; }
    public string Provider { get; set; } = "whisper";
    public string? ModelPath { get; set; }
    public string? AudioFile { get; set; }
    public bool ShowHelp { get; set; }
}
