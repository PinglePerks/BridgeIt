using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class AcolResponderAfter2CSuitRebidTests
{
    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    /// <summary>
    /// Creates context: North opened 2♣, South bid 2♦ (waiting), North rebid a suit.
    /// South (responder) to continue. Opponents pass throughout.
    /// </summary>
    private static DecisionContext CreateResponderContext(
        Suit openerRebidSuit, int openerRebidLevel,
        int hcp, Dictionary<Suit, int> shape, Suit longestSuit)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(openerRebidLevel, openerRebidSuit)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 8,
            LongestAndStrongest = longestSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, new TableKnowledge(Seat.South));
    }

    // =============================================================
    // Context applicability
    // =============================================================

    [Test]
    public void CouldMakeBid_TrueAfter2C_2D_SuitRebid()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 5, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void CouldMakeBid_FalseWhenOpenerRebid2NT()
    {
        // After 2C-2D-2NT, this rule should NOT fire (Stayman/Transfer handle it)
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(2)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 5, Shape = Shape(3, 3, 4, 3), IsBalanced = true,
            Losers = 8, LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var biddingCtx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var ctx = new DecisionContext(biddingCtx, handEval, aucEval, new TableKnowledge(Seat.South));

        var rule = new AcolResponderAfter2CSuitRebid();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void CouldMakeBid_FalseWhenNotResponder()
    {
        // From opener's perspective — should not fire
        var history = new AuctionHistory(Seat.North);
        var handEval = new HandEvaluation
        {
            Hcp = 5, Shape = Shape(3, 3, 4, 3), IsBalanced = false,
            Losers = 8, LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var biddingCtx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var ctx = new DecisionContext(biddingCtx, handEval, aucEval, new TableKnowledge(Seat.North));

        var rule = new AcolResponderAfter2CSuitRebid();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================================
    // Apply: fit cases — raise opener's suit
    // =============================================================

    [Test]
    public void Apply_3PlusHearts_After2H_Raises_To3H()
    {
        // 2C-2D-2H, responder has 3 hearts → 3H
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 5, Shape(4, 3, 4, 2), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void Apply_4PlusSpades_After2S_Raises_To3S()
    {
        // 2C-2D-2S, responder has 4 spades → 3S
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Spades, 2, 3, Shape(4, 2, 4, 3), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void Apply_3PlusClubs_After3C_Raises_To4C()
    {
        // 2C-2D-3C, responder has 3 clubs → 4C
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Clubs, 3, 4, Shape(3, 4, 3, 3), Suit.Hearts);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Clubs)));
    }

    [Test]
    public void Apply_3PlusDiamonds_After3D_Raises_To4D()
    {
        // 2C-2D-3D, responder has 3 diamonds → 4D
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Diamonds, 3, 6, Shape(3, 2, 5, 3), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Diamonds)));
    }

    // =============================================================
    // Apply: no fit — show own 5+ card suit
    // =============================================================

    [Test]
    public void Apply_NoFitFor2H_5PlusSpades_Bids2S()
    {
        // 2C-2D-2H, responder has 2 hearts but 5 spades → 2S
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 7, Shape(5, 2, 4, 2), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void Apply_NoFitFor2S_5PlusDiamonds_Bids3D()
    {
        // 2C-2D-2S, responder has 1 spade, 5 diamonds → 3D
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Spades, 2, 8, Shape(1, 3, 5, 4), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Diamonds)));
    }

    [Test]
    public void Apply_NoFitFor2H_5PlusSpadesAnd5PlusDiamonds_BidsSpades()
    {
        // With two 5-card suits, bids highest ranking first (spades)
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 6, Shape(5, 1, 5, 2), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    // =============================================================
    // Apply: no fit, no 5-card suit — bid NT
    // =============================================================

    [Test]
    public void Apply_NoFitNoSuit_After2H_Bids2NT()
    {
        // 2C-2D-2H, responder has 2 hearts, no 5-card suit → 2NT
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 4, Shape(4, 2, 4, 3), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Apply_NoFitNoSuit_After3D_Bids3NT()
    {
        // 2C-2D-3D, responder has 2 diamonds, no 5-card suit → 3NT
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Diamonds, 3, 5, Shape(4, 4, 2, 3), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    // =============================================================
    // Backward inference
    // =============================================================

    [Test]
    public void CouldExplainBid_TrueForSuitRaise()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 5, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void CouldExplainBid_TrueForNTBid()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 5, Shape(3, 2, 4, 4), Suit.Diamonds);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void GetConstraintForBid_RaiseShows3PlusSupport()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 5, Shape(3, 3, 4, 3), Suit.Diamonds);
        var info = rule.GetConstraintForBid(Bid.SuitBid(3, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<SuitLengthConstraint>());

        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().First();
        Assert.That(suitLen.MinLen, Is.EqualTo(3));
    }

    [Test]
    public void GetConstraintForBid_NewSuitShows5PlusLength()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 7, Shape(5, 2, 4, 2), Suit.Spades);
        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<SuitLengthConstraint>());

        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().First();
        Assert.That(suitLen.MinLen, Is.EqualTo(5));
    }

    [Test]
    public void GetConstraintForBid_MinLevelNT_ShowsWeakHcp()
    {
        var rule = new AcolResponderAfter2CSuitRebid();
        var ctx = CreateResponderContext(Suit.Hearts, 2, 4, Shape(4, 2, 4, 3), Suit.Spades);
        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        var hcp = composite.Constraints.OfType<HcpConstraint>().First();
        Assert.That(hcp.Max, Is.EqualTo(7));
    }
}
