using System.Text.Json;

namespace VoiceToText.Vosk;

/// <summary>Builds the JSON phrase list Vosk takes as a recognizer grammar.</summary>
internal static class VoskGrammar
{
    /// <summary>The token Vosk returns for speech outside the grammar.</summary>
    public const string Unknown = "[unk]";

    /// <summary>
    /// Returns the grammar JSON for these phrases, or null when there are none (free vocabulary).
    /// Phrases are trimmed, lower-cased and de-duplicated; Vosk vocabularies are lower case.
    /// </summary>
    public static string? ToJson(IEnumerable<string>? phrases, bool allowUnknown)
    {
        if (phrases is null)
            return null;

        var list = phrases
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (list.Count == 0)
            return null;

        if (allowUnknown && !list.Contains(Unknown))
            list.Add(Unknown);

        return JsonSerializer.Serialize(list);
    }

    /// <summary>Per-operation phrases win over the configured default.</summary>
    public static string? For(VoskRecognizerOptions options, VoiceToText.Models.RecognizerOptions? operation) =>
        ToJson(operation?.Phrases ?? options.Grammar, options.AllowUnknownWords);
}
