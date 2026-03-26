using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Competitive;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class OvercallRuleTests
{
    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a context where North opens with the given bid, and East is the overcaller (direct seat).
    /// </summary>
    private static DecisionContext CreateDirectOvercallContext(
        Bid openingBid, int hcp, Dictionary<Suit, int> shape,
        Suit? longestSuit = null, Dictionary<Suit, StopperQuality>? stoppers = null)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, openingBid));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = shape.Values.OrderByDescending(v => v).ToArray() is [4, 3, 3, 3] or [4, 4, 3, 2] or [5, 3, 3, 2],
            Losers = 7,
            LongestAndStrongest = longestSuit ?? shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key,
            SuitStoppers = stoppers ?? new Dictionary<Suit, StopperQuality>()
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.East);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.East, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a context in protective seat: N opens, E passes, S passes, W to bid.
    /// </summary>
    private static DecisionContext CreateProtectiveOvercallContext(
        Bid openingBid, int hcp, Dictionary<Suit, int> shape,
        Suit? longestSuit = null, Dictionary<Suit, StopperQuality>? stoppers = null)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, openingBid));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = shape.Values.OrderByDescending(v => v).ToArray() is [4, 3, 3, 3] or [4, 4, 3, 2] or [5, 3, 3, 2],
            Losers = 7,
            LongestAndStrongest = longestSuit ?? shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key,
            SuitStoppers = stoppers ?? new Dictionary<Suit, StopperQuality>()
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.West);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.West, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // ═══════════════════════════════════════════════════════════════
    // SIMPLE OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SimpleOvercall_1Level_OverOpponentOpening()
    {
        // N opens 1H. East has 10 HCP and 5 spades → overcall 1S.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void SimpleOvercall_2Level_WhenSuitBelowOpening()
    {
        // N opens 1S. East has 10 HCP and 5 hearts → overcall 2H.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Spades), 10, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void SimpleOvercall_TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 6, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void SimpleOvercall_TooManyHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 17, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void SimpleOvercall_Only4CardSuit_DirectSeat_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void SimpleOvercall_4CardSuit_ProtectiveSeat_Applies()
    {
        // Protective seat allows 4-card overcalls
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateProtectiveOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new SimpleOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void SimpleOvercall_BackwardInference_ConstraintsForBid()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new SimpleOvercallRule();
        var info = rule.GetConstraintForBid(Bid.SuitBid(1, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Bid, Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    // ═══════════════════════════════════════════════════════════════
    // JUMP OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void JumpOvercall_Intermediate_WithGoodSuit()
    {
        // N opens 1H. East has 14 HCP and 6 spades → jump to 2S.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 6 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 14, shape);

        var rule = new JumpOvercallRule("Intermediate", 12, 16, 6);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        // Cheapest for spades over 1H is 1S. Jump = 2S.
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void JumpOvercall_Weak_WithLongSuit()
    {
        // N opens 1S. East has 7 HCP and 6 hearts → jump to 3H.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 1 }, { Suit.Hearts, 6 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 2 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Spades), 7, shape);

        var rule = new JumpOvercallRule("Weak", 6, 10, 6);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        // Cheapest for hearts over 1S is 2H. Jump = 3H.
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void JumpOvercall_TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 6 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new JumpOvercallRule("Intermediate", 12, 16, 6);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void JumpOvercall_SuitTooShort_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 14, shape);

        var rule = new JumpOvercallRule("Intermediate", 12, 16, 6);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void JumpOvercall_BackwardInference()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 6 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 14, shape);

        var rule = new JumpOvercallRule("Intermediate", 12, 16, 6);
        // 2S is a jump over 1H (cheapest = 1S, jump = 2S)
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.True);
        // 1S is NOT a jump — it's the cheapest
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Spades), ctx), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // NT OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NTOvercall_Direct_BalancedWithStopper()
    {
        // N opens 1H. East has 16 HCP, balanced, stopper in hearts.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 16, shape, stoppers: stoppers);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void NTOvercall_Direct_NoStopper_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 16, shape);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTOvercall_Direct_NotBalanced_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 2 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 16, shape, stoppers: stoppers);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTOvercall_Direct_TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 13, shape, stoppers: stoppers);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTOvercall_Protective_LowerHcpRange()
    {
        // Protective seat: 12 HCP balanced with stopper should work.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateProtectiveOvercallContext(Bid.SuitBid(1, Suit.Hearts), 12, shape, stoppers: stoppers);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTOvercall_OverNTOpening_DoesNotApply()
    {
        // NT overcall only applies over suit openings
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateDirectOvercallContext(Bid.NoTrumpsBid(1), 16, shape);

        var rule = new NTOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTOvercall_BackwardInference()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateDirectOvercallContext(Bid.SuitBid(1, Suit.Hearts), 16, shape, stoppers: stoppers);

        var rule = new NTOvercallRule(15, 17, 12, 14);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);

        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(1), ctx);
        Assert.That(info, Is.Not.Null);
    }

    // ═══════════════════════════════════════════════════════════════
    // LOADER INTEGRATION
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Loader_Foundation_Includes_OvercallRules()
    {
        var systemsDir = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "BridgeIt.Systems", "Systems");
        var loader = new BridgeIt.Systems.BiddingSystemLoader();
        var loaded = loader.LoadFromFile(Path.Combine(systemsDir, "acol-foundation.json"));

        var ruleNames = loaded.Rules.Select(r => r.Name).ToList();
        Assert.That(ruleNames, Does.Contain("Simple Overcall"));
        Assert.That(ruleNames, Does.Contain("Jump Overcall (Intermediate)"));
        Assert.That(ruleNames, Does.Contain("1NT Overcall"));
    }
}
