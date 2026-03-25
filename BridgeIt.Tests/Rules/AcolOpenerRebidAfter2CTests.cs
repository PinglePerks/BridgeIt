using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class AcolOpenerRebidAfter2CTests
{
    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    /// <summary>
    /// Creates context: North opened 2C, East passed, South bid 2D (waiting), West passed.
    /// North (opener) to rebid.
    /// </summary>
    private static DecisionContext Create2CRebidContext(
        int hcp, bool balanced, Dictionary<Suit, int> shape, Suit longestSuit)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = balanced,
            Losers = 4,
            LongestAndStrongest = longestSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, new TableKnowledge(Seat.North));
    }

    // =============================================================
    // Context applicability
    // =============================================================

    [Test]
    public void CouldMakeBid_TrueAfter2C_2D()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(23, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void CouldMakeBid_FalseWhenNotOpener()
    {
        var rule = new AcolOpenerRebidAfter2C();
        // Create a context where it's round 1, not a rebid
        var history = new AuctionHistory(Seat.North);
        var handEval = new HandEvaluation
        {
            Hcp = 23, IsBalanced = true, Shape = Shape(3, 3, 4, 3),
            Losers = 4, LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var biddingCtx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var ctx = new DecisionContext(biddingCtx, handEval, aucEval, new TableKnowledge(Seat.North));
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================================
    // Balanced rebids
    // =============================================================

    [Test]
    public void Apply_Balanced23Hcp_Returns2NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(23, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Apply_Balanced24Hcp_Returns2NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(24, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Apply_Balanced25Hcp_Returns3NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(25, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    [Test]
    public void Apply_Balanced28Hcp_Returns3NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(28, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    // =============================================================
    // Unbalanced rebids — bid longest suit
    // =============================================================

    [Test]
    public void Apply_Unbalanced_BidsLongestSuit()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(22, false, Shape(2, 6, 3, 2), Suit.Hearts);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void Apply_Unbalanced_BidsSpades()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(20, false, Shape(6, 3, 2, 2), Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void Apply_Unbalanced_BidsDiamonds()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(21, false, Shape(2, 2, 6, 3), Suit.Diamonds);
        // 2D is the current contract, so next diamond bid is 3D
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Diamonds)));
    }

    // =============================================================
    // Backward inference (CouldExplainBid / GetConstraintForBid)
    // =============================================================

    [Test]
    public void CouldExplainBid_TrueFor2NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(23, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void CouldExplainBid_TrueFor3NT()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(25, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(3), ctx), Is.True);
    }

    [Test]
    public void CouldExplainBid_TrueForSuitBid()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(22, false, Shape(2, 6, 3, 2), Suit.Hearts);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void GetConstraintForBid_2NT_Shows23To24Balanced()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(23, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<BalancedConstraint>());
        Assert.That(composite.Constraints, Has.Some.TypeOf<HcpConstraint>());

        var hcp = composite.Constraints.OfType<HcpConstraint>().First();
        Assert.That(hcp.Min, Is.EqualTo(23));
        Assert.That(hcp.Max, Is.EqualTo(24));
    }

    [Test]
    public void GetConstraintForBid_3NT_Shows25PlusBalanced()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(25, true, Shape(3, 3, 4, 3), Suit.Diamonds);
        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(3), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<BalancedConstraint>());

        var hcp = composite.Constraints.OfType<HcpConstraint>().First();
        Assert.That(hcp.Min, Is.EqualTo(25));
    }

    [Test]
    public void GetConstraintForBid_SuitBid_Shows20PlusWithSuitLength()
    {
        var rule = new AcolOpenerRebidAfter2C();
        var ctx = Create2CRebidContext(22, false, Shape(2, 6, 3, 2), Suit.Hearts);
        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = (CompositeConstraint)info!.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<HcpConstraint>());
        Assert.That(composite.Constraints, Has.Some.TypeOf<SuitLengthConstraint>());

        var hcp = composite.Constraints.OfType<HcpConstraint>().First();
        Assert.That(hcp.Min, Is.EqualTo(20));
    }
}
