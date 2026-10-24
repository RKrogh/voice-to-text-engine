// Each call below goes through P/Invoke, so it fails with DllNotFoundException when the
// package did not bring its native library along.
Vosk.Vosk.SetLogLevel(-1);
Console.WriteLine("libvosk loaded");

Console.WriteLine($"whisper loaded: {Whisper.net.WhisperFactory.GetRuntimeInfo()}");
