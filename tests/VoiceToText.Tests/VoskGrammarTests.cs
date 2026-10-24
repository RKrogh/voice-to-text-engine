using System.Text.Json;
using VoiceToText.Models;
using VoiceToText.Vosk;
using Xunit;

namespace VoiceToText.Tests;

public class VoskGrammarTests
{
    [Fact]
    public void No_phrases_means_free_vocabulary()
    {
        Assert.Null(VoskGrammar.ToJson(null, allowUnknown: true));
        Assert.Null(VoskGrammar.ToJson(["", "  "], allowUnknown: true));
    }

    [Fact]
    public void Phrases_are_normalised_and_unknown_is_appended()
    {
        var json = VoskGrammar.ToJson([" Roll ", "roll", "Kessler Station"], allowUnknown: true);

        Assert.Equal(["roll", "kessler station", "[unk]"], Parse(json));
    }

    [Fact]
    public void Unknown_can_be_left_out()
    {
        var json = VoskGrammar.ToJson(["roll"], allowUnknown: false);

        Assert.Equal(["roll"], Parse(json));
    }

    [Fact]
    public void Operation_phrases_override_the_configured_grammar()
    {
        var options = new VoskRecognizerOptions { ModelPath = "m", Grammar = ["pitch"] };

        var json = VoskGrammar.For(options, new RecognizerOptions { Phrases = ["yaw"] });

        Assert.Equal(["yaw", "[unk]"], Parse(json));
        Assert.Equal(["pitch", "[unk]"], Parse(VoskGrammar.For(options, null)));
    }

    private static string[] Parse(string? json) => JsonSerializer.Deserialize<string[]>(json!)!;
}
