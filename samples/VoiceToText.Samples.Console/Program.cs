using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceToText.Abstractions;
using VoiceToText.DependencyInjection;
using Whisper.net.Ggml;

var micMode = args.Contains("--mic", StringComparer.OrdinalIgnoreCase);
var filteredArgs = args.Where(a => !a.Equals("--mic", StringComparison.OrdinalIgnoreCase)).ToArray();

if (micMode)
{
    await RunMicMode(filteredArgs);
}
else
{
    await RunFileMode(filteredArgs);
}

static async Task RunFileMode(string[] args)
{
    // --- Parse arguments ---
    var defaultWav = Path.Combine(AppContext.BaseDirectory, "hello-world.wav");
    var wavPath = args.Length > 0 ? args[0] : defaultWav;
    var modelPath = args.Length > 1 ? args[1] : "ggml-tiny.bin";

    if (!File.Exists(wavPath) && args.Length == 0)
    {
        PrintUsage();
        return;
    }

    if (!File.Exists(wavPath))
    {
        Console.Error.WriteLine($"Error: WAV file not found: {wavPath}");
        return;
    }

    var (services, modelPathResolved) = await SetupServices(modelPath);
    services.AddVoiceToText().AddWhisperRecognizer(opts => opts.ModelPath = modelPathResolved);

    await using var provider = services.BuildServiceProvider();

    // --- Transcribe ---
    await using var recognizer = provider.GetRequiredService<ISpeechRecognizer>();
    await using var audioStream = File.OpenRead(wavPath);

    Console.WriteLine($"Transcribing: {wavPath}");
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

static async Task RunMicMode(string[] args)
{
    var modelPath = args.Length > 0 ? args[0] : "ggml-tiny.bin";

    var (services, modelPathResolved) = await SetupServices(modelPath);
    services.AddVoiceToText()
        .AddWhisperRecognizer(opts => opts.ModelPath = modelPathResolved)
        .AddNAudioMicrophone();

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

    Console.WriteLine("Listening... Press Enter to stop.");
    Console.WriteLine();
    Console.ReadLine();

    await audioSource.StopAsync();
    await streamingRecognizer.StopAsync();

    Console.WriteLine();
    Console.WriteLine("Done.");
}

static async Task<(ServiceCollection services, string modelPath)> SetupServices(string modelPath)
{
    // --- Download model if needed ---
    if (!File.Exists(modelPath))
    {
        Console.WriteLine($"Model not found at '{modelPath}'. Downloading ggml-tiny model...");
        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Tiny);
        using var fileWriter = File.OpenWrite(modelPath);
        await modelStream.CopyToAsync(fileWriter);
        Console.WriteLine("Download complete.");
    }

    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

    return (services, modelPath);
}

static void PrintUsage()
{
    Console.WriteLine(
        "Usage: dotnet run --project samples/VoiceToText.Samples.Console -- [audio.wav] [model-path]"
    );
    Console.WriteLine(
        "       dotnet run --project samples/VoiceToText.Samples.Console -- --mic [model-path]"
    );
    Console.WriteLine();
    Console.WriteLine("  [audio.wav]    Path to a WAV file to transcribe (default: hello-world.wav)");
    Console.WriteLine("  [model-path]   Path to a Whisper GGML model (default: ggml-tiny.bin)");
    Console.WriteLine("  --mic          Live microphone streaming mode (Windows only, requires NAudio)");
    Console.WriteLine();
    Console.WriteLine(
        "If the model file doesn't exist, it will be downloaded automatically (~75 MB for tiny)."
    );
}
