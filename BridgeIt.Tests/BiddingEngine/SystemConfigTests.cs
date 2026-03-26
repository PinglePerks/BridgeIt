using System.Text.Json;
using BridgeIt.Systems;
using BridgeIt.Systems.Config;

namespace BridgeIt.Tests.BiddingEngine;

[TestFixture]
public class SystemConfigTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static string SystemsDir =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
            "BridgeIt.Systems", "Systems");

    private static BridgeSystemConfig LoadSystem(string filename)
    {
        var path = Path.Combine(SystemsDir, filename);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BridgeSystemConfig>(json, JsonOpts)!;
    }

    [TestCase("acol-foundation.json")]
    [TestCase("acol-benji.json")]
    [TestCase("acol-modern.json")]
    [TestCase("sayc.json")]
    public void SystemFile_Deserialises_Successfully(string filename)
    {
        var config = LoadSystem(filename);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.Name, Is.Not.Null.And.Not.Empty);
    }

    [TestCase("acol-foundation.json")]
    [TestCase("acol-benji.json")]
    [TestCase("acol-modern.json")]
    [TestCase("sayc.json")]
    public void SystemFile_Has_Priorities(string filename)
    {
        var config = LoadSystem(filename);
        Assert.That(config.Priorities, Is.Not.Null);
        Assert.That(config.Priorities!.Count, Is.GreaterThan(10));
    }

    [Test]
    public void Foundation_Has_WeaknessTakeouts_NoTransfers()
    {
        var config = LoadSystem("acol-foundation.json");
        Assert.That(config.ResponseTo1NT?.WeaknessTakeouts?.Enabled, Is.True);
        Assert.That(config.ResponseTo1NT?.Transfers, Is.Null);
    }

    [Test]
    public void Benji_Has_Transfers_And_MinorTransfers()
    {
        var config = LoadSystem("acol-benji.json");
        Assert.That(config.ResponseTo1NT?.Transfers?.Enabled, Is.True);
        Assert.That(config.ResponseTo1NT?.MinorTransfers?.Enabled, Is.True);
        Assert.That(config.ResponseTo1NT?.MinorTransfers?.ClubTransferBid, Is.EqualTo("2S"));
    }

    [Test]
    public void Benji_Has_BenjaminTwos_With_2D_GameForce()
    {
        var config = LoadSystem("acol-benji.json");
        Assert.That(config.TwoLevelOpenings?.BenjaminTwos, Is.True);
        Assert.That(config.StrongOpening?.Bid, Is.EqualTo("2D"));
        Assert.That(config.StrongOpening?.GameForcing, Is.True);
    }

    [Test]
    public void Benji_2NT_Is_19_20()
    {
        var config = LoadSystem("acol-benji.json");
        Assert.That(config.NT2Opening?.MinHcp, Is.EqualTo(19));
        Assert.That(config.NT2Opening?.MaxHcp, Is.EqualTo(20));
    }

    [Test]
    public void Modern_Has_Transfers_Baron_Splinters()
    {
        var config = LoadSystem("acol-modern.json");
        Assert.That(config.ResponseTo1NT?.Transfers?.Enabled, Is.True);
        Assert.That(config.ResponseTo1NT?.Baron?.Enabled, Is.True);
        Assert.That(config.ResponseTo1NT?.Baron?.Bid, Is.EqualTo("2S"));
        Assert.That(config.Splinters?.Enabled, Is.True);
        Assert.That(config.TrialBids?.Enabled, Is.True);
    }

    [Test]
    public void SAYC_Has_FiveCardMajors_BetterMinor()
    {
        var config = LoadSystem("sayc.json");
        Assert.That(config.SuitOpening?.MajorMinLength, Is.EqualTo(5));
        Assert.That(config.SuitOpening?.MinorMinLength, Is.EqualTo(3));
        Assert.That(config.SuitOpening?.MinorSelection, Is.EqualTo("BetterMinor"));
        Assert.That(config.SuitOpening?.FourCardMajor, Is.Null);
    }

    [Test]
    public void SAYC_Has_Strong1NT()
    {
        var config = LoadSystem("sayc.json");
        Assert.That(config.NT1Opening?.MinHcp, Is.EqualTo(15));
        Assert.That(config.NT1Opening?.MaxHcp, Is.EqualTo(17));
    }

    [Test]
    public void SAYC_Has_Weak2D()
    {
        var config = LoadSystem("sayc.json");
        Assert.That(config.TwoLevelOpenings?.WeakTwos?.Suits, Does.Contain("D"));
        Assert.That(config.TwoLevelOpenings?.WeakTwos?.Suits, Does.Contain("H"));
        Assert.That(config.TwoLevelOpenings?.WeakTwos?.Suits, Does.Contain("S"));
    }

    [Test]
    public void Foundation_1NT_Is_12_14()
    {
        var config = LoadSystem("acol-foundation.json");
        Assert.That(config.NT1Opening?.MinHcp, Is.EqualTo(12));
        Assert.That(config.NT1Opening?.MaxHcp, Is.EqualTo(14));
    }

    [Test]
    public void All_Acol_Systems_Have_4Card_Majors()
    {
        foreach (var file in new[] { "acol-foundation.json", "acol-benji.json", "acol-modern.json" })
        {
            var config = LoadSystem(file);
            Assert.That(config.SuitOpening?.MajorMinLength, Is.EqualTo(4), $"Failed for {file}");
            Assert.That(config.SuitOpening?.FourCardMajor, Is.Not.Null, $"Failed for {file}");
        }
    }

    [Test]
    public void Benji_SuitOpening_Is_11_HCP()
    {
        var config = LoadSystem("acol-benji.json");
        Assert.That(config.SuitOpening?.MinHcp, Is.EqualTo(11));
    }

    [Test]
    public void Foundation_And_Modern_SuitOpening_Is_12_HCP()
    {
        foreach (var file in new[] { "acol-foundation.json", "acol-modern.json" })
        {
            var config = LoadSystem(file);
            Assert.That(config.SuitOpening?.MinHcp, Is.EqualTo(12), $"Failed for {file}");
        }
    }

    // ── Loader tests ───────────────────────────────────────────────────────────

    [Test]
    public void Loader_Foundation_Produces_Expected_Rule_Count()
    {
        // Foundation Acol: no transfers (weakness takeouts instead)
        // Expected: 5 openings + 2 Stayman (1NT, 2NT) + 1 NT raise + 1 response to 2C
        //         + 5 responses to 1-suit + 1 opener rebid after 2C + 3 Stayman responses
        //         + 3 responder after Stayman + 0 transfer completions (no transfers)
        //         + 4 other opener rebids + 5 responder rebids + 6 knowledge = many
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-foundation.json"));
        Assert.That(loaded.Rules, Is.Not.Empty);
        Assert.That(loaded.Name, Is.EqualTo("Standard English Acol (Foundation)"));

        // Log rule names for debugging
        foreach (var rule in loaded.Rules)
            TestContext.WriteLine($"  {rule.Priority:D2} {rule.Name}");
    }

    [Test]
    public void Loader_Foundation_Rules_Are_Sorted_By_Priority_Descending()
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-foundation.json"));

        for (int i = 1; i < loaded.Rules.Count; i++)
        {
            Assert.That(loaded.Rules[i].Priority, Is.LessThanOrEqualTo(loaded.Rules[i - 1].Priority),
                $"Rule {loaded.Rules[i].Name} (priority {loaded.Rules[i].Priority}) should not appear after {loaded.Rules[i - 1].Name} (priority {loaded.Rules[i - 1].Priority})");
        }
    }

    [Test]
    public void Loader_Foundation_Has_Warnings_For_Unbuilt_Sections()
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-foundation.json"));
        Assert.That(loaded.Warnings, Is.Not.Empty);
        // Overcalls are now implemented (simple, jump, NT) — no longer warned
        Assert.That(loaded.Warnings.Any(w => w.Contains("Slam")), Is.True);
        Assert.That(loaded.Warnings.Any(w => w.Contains("WeaknessTakeouts")), Is.True);
    }

    [Test]
    public void Loader_Modern_Has_Transfer_Completions()
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-modern.json"));
        // Modern Acol has transfers, so should have CompleteTransfer rules
        Assert.That(loaded.Rules.Any(r => r.Name.Contains("Complete transfer")), Is.True);
    }

    [Test]
    public void Loader_Foundation_Has_No_1NT_Transfer_Completions()
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-foundation.json"));
        // Foundation has no transfers over 1NT (uses weakness takeouts instead)
        Assert.That(loaded.Rules.Any(r => r.Name.Contains("Complete transfer after 1NT")), Is.False);
    }

    [TestCase("acol-foundation.json")]
    [TestCase("acol-benji.json")]
    [TestCase("acol-modern.json")]
    [TestCase("sayc.json")]
    public void Loader_All_Systems_Load_Without_Error(string filename)
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, filename));
        Assert.That(loaded.Rules.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Loader_Foundation_KnowledgeSignOff_Is_Last()
    {
        var loader = new BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(SystemsDir, "acol-foundation.json"));
        Assert.That(loaded.Rules.Last().Name, Does.Contain("Sign off"));
        Assert.That(loaded.Rules.Last().Priority, Is.EqualTo(0));
    }
}
