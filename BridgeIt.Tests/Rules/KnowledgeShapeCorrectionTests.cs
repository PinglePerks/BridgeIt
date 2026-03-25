using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Knowledge;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class KnowledgeShapeCorrectionTests
{
    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    /// <summary>
    /// Creates context for opener's round 3 after: 1♠ – 1NT – 2♥ – 2NT – ?
    /// North (opener) has shown spades (4+) and hearts (4+) via bids.
    /// Responder bid 2NT (invite), so opener has round 3 decision.
    /// Me.MinShape is set to simulate what knowledge evaluator would produce.
    /// </summary>
    private static DecisionContext CreateOpenerRound3Context(
        int hcp, Dictionary<Suit, int> shape, Suit longestSuit,
        Dictionary<Suit, int> meMinShape,
        Bid? partnerLastNonPassBid = null)
    {
        // 1♠ (North) – Pass – 1NT (South) – Pass – 2♥ (North) – Pass – 2NT (South) – Pass – ?
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(2)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 6,
            LongestAndStrongest = longestSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);

        // Set Me.MinShape to simulate accumulated bid inference
        foreach (var kvp in meMinShape)
            tableKnowledge.Me.MinShape[kvp.Key] = kvp.Value;

        // Partner communicated via 1NT
        tableKnowledge.Partner.HcpMin = 6;
        tableKnowledge.Partner.HcpMax = 9;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a generic context with customisable auction and Me.MinShape.
    /// </summary>
    private static DecisionContext CreateContextWithKnowledge(
        Seat seat, AuctionHistory history,
        int hcp, Dictionary<Suit, int> shape, Suit longestSuit,
        Dictionary<Suit, int> meMinShape,
        int partnerHcpMin = 0, int partnerHcpMax = 37)
    {
        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 6,
            LongestAndStrongest = longestSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(seat);

        foreach (var kvp in meMinShape)
            tableKnowledge.Me.MinShape[kvp.Key] = kvp.Value;

        tableKnowledge.Partner.HcpMin = partnerHcpMin;
        tableKnowledge.Partner.HcpMax = partnerHcpMax;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, seat, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // =============================================================
    // Core: shape correction fires with hidden length
    // =============================================================

    [Test]
    public void Apply_5_5_OpenerWith5Hearts_Shown4_Bids3S()
    {
        // Opener has 5♠ 5♥, bid 1♠ then 2♥. Partner thinks 4+ in each.
        // Both have equal hidden extra (1), so spades wins tiebreak (higher rank).
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(5, 5, 2, 1),
            longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0) // Shown 4+ spades, 4+ hearts
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void Apply_6CardSuit_Shown5_Rebids()
    {
        // After 1♥ – (resp) – 2♥, opener has 6 hearts but only showed 5.
        // Current contract is 2♥. Rebid should be 3♥.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateContextWithKnowledge(
            Seat.North, history,
            hcp: 14, shape: Shape(2, 6, 3, 2), longestSuit: Suit.Hearts,
            meMinShape: Shape(0, 5, 0, 0), // Rebid hearts showed 5+
            partnerHcpMin: 6, partnerHcpMax: 12
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    // =============================================================
    // Should NOT fire
    // =============================================================

    [Test]
    public void CouldMakeBid_FalseWhenNoHiddenLength()
    {
        // Opener has exactly 4♠ 4♥, already shown 4+. No correction needed.
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(4, 4, 3, 2),
            longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0) // Shown matches actual
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void CouldMakeBid_FalseWhenShortSuit()
    {
        // Opener has 4 spades, shown 3+. Hidden length exists (4 > 3)
        // but actual is only 4, below the 5-card minimum for correction.
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(4, 3, 3, 3),
            longestSuit: Suit.Spades,
            meMinShape: Shape(3, 0, 0, 0) // Only shown 3+
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void CouldMakeBid_IgnoresUnbidSuit_CorrectsBidSuit()
    {
        // Opener has 6 diamonds but never bid them (Me.MinShape[D] = 0).
        // Diamonds aren't corrected — it's a new suit, not a correction.
        // But spades (actual 5 > shown 4) IS corrected.
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(5, 2, 6, 0),
            longestSuit: Suit.Diamonds,
            meMinShape: Shape(4, 0, 0, 0) // Only shown spades
        );

        // Spades: actual 5 > shown 4, actual >= 5 → fires for spades
        // Diamonds: shown 0 → never bid, skip
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void CouldMakeBid_FalseWhenGameLevel()
    {
        // Contract is at 3♠, next spade bid would be 4♠ (game) — don't fire.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(3, Suit.Spades)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateContextWithKnowledge(
            Seat.North, history,
            hcp: 14, shape: Shape(6, 3, 2, 2), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 0, 0, 0), // Shown 4+ spades, have 6
            partnerHcpMin: 10, partnerHcpMax: 12
        );

        // 4♠ would be game level — should not fire
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void CouldMakeBid_FalsePreOpening()
    {
        var history = new AuctionHistory(Seat.North);
        var rule = new KnowledgeShapeCorrection();

        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = Shape(5, 5, 2, 1), IsBalanced = false,
            Losers = 6, LongestAndStrongest = Suit.Spades
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var biddingCtx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var ctx = new DecisionContext(biddingCtx, handEval, aucEval, new TableKnowledge(Seat.North));

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void CouldMakeBid_FalseWithoutPartnerCommunication()
    {
        // Partner hasn't bid — no communication to correct against
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateContextWithKnowledge(
            Seat.North, history,
            hcp: 13, shape: Shape(6, 5, 1, 1), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 0, 0, 0)
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================================
    // Suit selection
    // =============================================================

    [Test]
    public void Apply_PrefersGreaterHiddenLength()
    {
        // 5♠ shown 4 (extra=1), 6♥ shown 4 (extra=2) → picks hearts
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(5, 6, 1, 1),
            longestSuit: Suit.Hearts,
            meMinShape: Shape(4, 4, 0, 0)
        );

        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void Apply_TiebreaksOnRank()
    {
        // 5♠ shown 4 (extra=1), 5♥ shown 4 (extra=1) → picks spades (higher rank)
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13,
            shape: Shape(5, 5, 2, 1),
            longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0)
        );

        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    // =============================================================
    // Responder can also correct
    // =============================================================

    [Test]
    public void Apply_ResponderCorrects5CardSuit()
    {
        // After 1♣ – 1♠ – 2♣ – 2♠, responder has 5♠ shown as 4+.
        // Round 3 for responder.
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Spades)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.Pass()));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateContextWithKnowledge(
            Seat.South, history,
            hcp: 10, shape: Shape(5, 3, 3, 2), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 0, 0, 0), // Shown 4+ spades
            partnerHcpMin: 12, partnerHcpMax: 15
        );

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    // =============================================================
    // Backward inference
    // =============================================================

    [Test]
    public void CouldExplainBid_TrueForSuitBid()
    {
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13, shape: Shape(5, 5, 2, 1), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0)
        );

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void CouldExplainBid_FalseForNTBid()
    {
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13, shape: Shape(5, 5, 2, 1), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0)
        );

        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void GetConstraintForBid_ShowsExtraLength()
    {
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13, shape: Shape(5, 5, 2, 1), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0)
        );

        var info = rule.GetConstraintForBid(Bid.SuitBid(3, Suit.Hearts), ctx);
        Assert.That(info, Is.Not.Null);

        var composite = (CompositeConstraint)info!.Constraint!;
        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().First();

        // Me.MinShape[Hearts] = 4, so correction infers 5+ (max of currentMin+1 and 5)
        Assert.That(suitLen.MinLen, Is.EqualTo(5));
    }

    [Test]
    public void GetConstraintForBid_ReturnsNullForNonSuit()
    {
        var rule = new KnowledgeShapeCorrection();
        var ctx = CreateOpenerRound3Context(
            hcp: 13, shape: Shape(5, 5, 2, 1), longestSuit: Suit.Spades,
            meMinShape: Shape(4, 4, 0, 0)
        );

        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);
        Assert.That(info, Is.Null);
    }
}
