using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Competitive;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class TakeoutDoubleTests
{
    private static DecisionContext CreateOvercallContext(
        Bid openingBid, int hcp, Dictionary<Suit, int> shape, Seat dealer = Seat.North)
    {
        // East is the overcaller in direct seat
        var history = new AuctionHistory(dealer);
        history.Add(new AuctionBid(Seat.North, openingBid));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key,
            SuitStoppers = new Dictionary<Suit, StopperQuality>()
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.East);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.East, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    [Test]
    public void ClassicShape_ShortInOpponentSuit_SupportForUnbid()
    {
        // N opens 1H. East: 13 HCP, 4-1-4-4 (short in hearts).
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 1 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 13, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.Double()));
    }

    [Test]
    public void ClassicShape_VoidInOpponentSuit()
    {
        // N opens 1D. East: 12 HCP, 4-4-0-5 (void in diamonds).
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 0 }, { Suit.Clubs, 5 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Diamonds), 12, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void StrongOverride_AnyShape_16Plus()
    {
        // N opens 1H. East: 18 HCP, doesn't have classic shape (3 hearts).
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 18, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 1 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NoShortageInOpponentSuit_BelowStrongOverride_DoesNotApply()
    {
        // 13 HCP but 3 cards in opponent's suit (not short enough) and not 16+ for override.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 13, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void OverNTOpening_DoesNotApply()
    {
        // Takeout double only over suit openings
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 1 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateOvercallContext(Bid.NoTrumpsBid(1), 13, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void BackwardInference_ExplainDouble()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 1 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 13, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldExplainBid(Bid.Double(), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Spades), ctx), Is.False);

        var info = rule.GetConstraintForBid(Bid.Double(), ctx);
        Assert.That(info, Is.Not.Null);
    }

    [Test]
    public void LacksSupport_ForUnbidSuit_DoesNotApply()
    {
        // N opens 1H. East: 12 HCP, 1-1-2-9. Short in hearts but only 2 diamonds (not 3+).
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 1 }, { Suit.Hearts, 1 }, { Suit.Diamonds, 2 }, { Suit.Clubs, 9 } };
        var ctx = CreateOvercallContext(Bid.SuitBid(1, Suit.Hearts), 12, shape);

        var rule = new TakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }
}
