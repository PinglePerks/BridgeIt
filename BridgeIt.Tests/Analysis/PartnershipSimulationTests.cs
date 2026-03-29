using BridgeIt.Analysis.Parsers;
using BridgeIt.Api.Services;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Extensions;
using BridgeIt.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Tests.Analysis;

[TestFixture]
public class PartnershipIdentificationTests
{
    [Test]
    public void IdentifyPartnership_KnownName_AtNorth_ReturnsNorthSouth()
    {
        var names = new Dictionary<Seat, string>
        {
            [Seat.North] = "gillfromtheboro",
            [Seat.East] = "Opponent1",
            [Seat.South] = "TheMole",
            [Seat.West] = "Opponent2"
        };

        var result = PbnParser.IdentifyPartnership(names);

        Assert.That(result, Is.Not.Null);
        // First match found is gillfromtheboro at North
        Assert.That(result!.Value.Seat1, Is.EqualTo(Seat.North));
        Assert.That(result.Value.Seat2, Is.EqualTo(Seat.South));
    }

    [Test]
    public void IdentifyPartnership_KnownName_AtEast_ReturnsEastWest()
    {
        var names = new Dictionary<Seat, string>
        {
            [Seat.North] = "Unknown1",
            [Seat.East] = "gillfromtheboro",
            [Seat.South] = "Unknown2",
            [Seat.West] = "TheMole"
        };

        var result = PbnParser.IdentifyPartnership(names);

        Assert.That(result, Is.Not.Null);
        var seats = new[] { result!.Value.Seat1, result.Value.Seat2 };
        Assert.That(seats, Does.Contain(Seat.East));
        Assert.That(seats, Does.Contain(Seat.West));
    }

    [Test]
    public void IdentifyPartnership_CaseInsensitive()
    {
        var names = new Dictionary<Seat, string>
        {
            [Seat.North] = "GILLFROMTHEBORO",
            [Seat.East] = "Opp1",
            [Seat.South] = "Opp2",
            [Seat.West] = "Opp3"
        };

        var result = PbnParser.IdentifyPartnership(names);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void IdentifyPartnership_NoMatch_ReturnsNull()
    {
        var names = new Dictionary<Seat, string>
        {
            [Seat.North] = "Alice",
            [Seat.East] = "Bob",
            [Seat.South] = "Charlie",
            [Seat.West] = "Dave"
        };

        var result = PbnParser.IdentifyPartnership(names);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void IdentifyPartnership_EmptyNames_ReturnsNull()
    {
        var names = new Dictionary<Seat, string>();
        var result = PbnParser.IdentifyPartnership(names);
        Assert.That(result, Is.Null);
    }
}

[TestFixture]
public class PartnershipSimulationServiceTests
{
    private PartnershipSimulationService _service = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddBridgeItCore();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        // Load the bidding system
        services.AddSingleton<BiddingSystemLoader>();
        services.AddSingleton(sp =>
        {
            var loader = sp.GetRequiredService<BiddingSystemLoader>();
            var systemPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "BridgeIt.Systems", "Systems", "acol-modern.json");
            return loader.LoadFromFile(systemPath);
        });
        services.AddSingleton<IEnumerable<IBiddingRule>>(sp =>
            sp.GetRequiredService<LoadedSystem>().Rules);

        var provider = services.BuildServiceProvider();

        _service = new PartnershipSimulationService(
            provider.GetRequiredService<IEnumerable<IBiddingRule>>(),
            provider.GetRequiredService<IRuleLookupService>(),
            provider.GetRequiredService<ILoggerFactory>());
    }

    [Test]
    public async Task CleanAuction_NoConflicts_ProducesCorrectStructure()
    {
        // Simple auction: N opens 1S, E passes, S responds, W passes, etc.
        // All 4 passes at end
        var hands = new Dictionary<Seat, BridgeIt.Core.Domain.Primatives.Hand>
        {
            [Seat.North] = "SAKJ54 HQ82 DT9 CK63".ToHand(),   // 13 HCP, 5 spades
            [Seat.East]  = "ST76 HJ54 D8743 CQ95".ToHand(),    // 4 HCP, pass
            [Seat.South] = "SQ83 HAK63 DK52 CJ42".ToHand(),    // 13 HCP
            [Seat.West]  = "S92 HT97 DAQJ6 CAT87".ToHand(),    // 10 HCP
        };

        // Played auction: N opens, E passes, S responds, W passes, N rebids, E passes, S passes, W passes
        var playedAuction = new List<string> { "1S", "Pass", "2H", "Pass", "2S", "Pass", "Pass", "Pass" };
        var ourSeats = (Seat.North, Seat.South);

        var result = await _service.Simulate(hands, playedAuction, Seat.North, Vulnerability.None, ourSeats);

        // No conflicts — opponents just passed
        Assert.That(result.Conflicts, Is.Empty);

        // Bids should alternate engine/played
        Assert.That(result.Bids.Count, Is.GreaterThan(0));

        // Our seats should be "engine", opponents should be "played"
        foreach (var bid in result.Bids)
        {
            if (bid.Seat == "North" || bid.Seat == "South")
                Assert.That(bid.Source, Is.EqualTo("engine"), $"Bid by {bid.Seat} should be engine");
            else
                Assert.That(bid.Source, Is.EqualTo("played"), $"Bid by {bid.Seat} should be played");
        }
    }

    [Test]
    public async Task ConflictDetection_OpponentBidBelowContract_RecordsConflict()
    {
        var hands = new Dictionary<Seat, BridgeIt.Core.Domain.Primatives.Hand>
        {
            [Seat.North] = "SAKJ54 HQ82 DT9 CK63".ToHand(),
            [Seat.East]  = "ST76 HJ54 D8743 CQ95".ToHand(),
            [Seat.South] = "SQ83 HAK63 DK52 CJ42".ToHand(),
            [Seat.West]  = "S92 HT97 DAQJ6 CAT87".ToHand(),
        };

        // Fabricate an auction where opponent's bid is illegally low
        // N opens (engine), E "bids" 1C (which may be below the engine's bid), etc.
        // The engine will likely open 1S for North. Then East's "1C" would be below 1S.
        var playedAuction = new List<string> { "1S", "1C", "2H", "Pass", "2S", "Pass", "Pass", "Pass" };
        var ourSeats = (Seat.North, Seat.South);

        var result = await _service.Simulate(hands, playedAuction, Seat.North, Vulnerability.None, ourSeats);

        // East's 1C should be flagged as a conflict (below 1S)
        Assert.That(result.Conflicts, Has.Count.GreaterThanOrEqualTo(1));
        var eastConflict = result.Conflicts.FirstOrDefault(c => c.Seat == "East");
        Assert.That(eastConflict, Is.Not.Null, "Expected a conflict for East's 1C bid");
        Assert.That(eastConflict!.RealBid, Is.EqualTo("1C"));

        // Bid should still be injected
        var eastBids = result.Bids.Where(b => b.Seat == "East").ToList();
        Assert.That(eastBids.Count, Is.GreaterThan(0));
        Assert.That(eastBids[0].Call, Is.EqualTo("1C"));
        Assert.That(eastBids[0].Source, Is.EqualTo("played"));
    }

    [Test]
    public async Task Simulation_RunsFullAuctionToCompletion()
    {
        var hands = new Dictionary<Seat, BridgeIt.Core.Domain.Primatives.Hand>
        {
            [Seat.North] = "SAKJ54 HQ82 DT9 CK63".ToHand(),
            [Seat.East]  = "ST76 HJ54 D8743 CQ95".ToHand(),
            [Seat.South] = "SQ83 HAK63 DK52 CJ42".ToHand(),
            [Seat.West]  = "S92 HT97 DAQJ6 CAT87".ToHand(),
        };

        // Opponents just pass — ReplayPlayer will Pass once queue is exhausted too
        var playedAuction = new List<string>
            { "1S", "Pass", "Pass", "Pass" };
        var ourSeats = (Seat.North, Seat.South);

        var result = await _service.Simulate(hands, playedAuction, Seat.North, Vulnerability.None, ourSeats);

        // Auction runs to natural completion (3 consecutive passes after a real bid)
        Assert.That(result.Bids.Count, Is.GreaterThanOrEqualTo(4));

        // Last 3 bids should be passes (auction termination)
        var lastThree = result.Bids.TakeLast(3).ToList();
        Assert.That(lastThree.All(b => b.Call == "Pass"), Is.True,
            "Auction should end with 3 consecutive passes");
    }

    [Test]
    public async Task Simulation_DebugLogs_OnlyForOurSeats()
    {
        var hands = new Dictionary<Seat, BridgeIt.Core.Domain.Primatives.Hand>
        {
            [Seat.North] = "SAKJ54 HQ82 DT9 CK63".ToHand(),
            [Seat.East]  = "ST76 HJ54 D8743 CQ95".ToHand(),
            [Seat.South] = "SQ83 HAK63 DK52 CJ42".ToHand(),
            [Seat.West]  = "S92 HT97 DAQJ6 CAT87".ToHand(),
        };

        var playedAuction = new List<string> { "1S", "Pass", "2H", "Pass", "2S", "Pass", "Pass", "Pass" };
        var ourSeats = (Seat.North, Seat.South);

        var result = await _service.Simulate(hands, playedAuction, Seat.North, Vulnerability.None, ourSeats);

        // Debug logs are collected for all engine bids (our seats only)
        foreach (var log in result.DebugLogs)
        {
            Assert.That(log.Seat, Is.AnyOf("North", "South"),
                $"Debug log for {log.Seat} should not be present — only our seats");
        }
    }
}
