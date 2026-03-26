using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class AdvanceAfterDoubleTests
{
    /// <summary>
    /// N opens 1H, E (partner) doubles, S passes, W (advancer) to bid.
    /// </summary>
    private static DecisionContext CreateAdvanceContext(
        Bid openingBid, int hcp, Dictionary<Suit, int> shape,
        Dictionary<Suit, StopperQuality>? stoppers = null)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, openingBid));
        history.Add(new AuctionBid(Seat.East, Bid.Double()));
        history.Add(new AuctionBid(Seat.South, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = shape.Values.OrderByDescending(v => v).ToArray() is [4, 3, 3, 3] or [4, 4, 3, 2] or [5, 3, 3, 2],
            Losers = 7,
            LongestAndStrongest = shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key,
            SuitStoppers = stoppers ?? new Dictionary<Suit, StopperQuality>()
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.West);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 40;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.West, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    [Test]
    public void MinimumResponse_WeakHand_BidsBestSuit()
    {
        // Partner doubled 1H. 5 HCP, 4 spades → bid 1S at minimum.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 5, shape);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void InvitationalJump_9To11Hcp()
    {
        // Partner doubled 1H. 10 HCP, 4 spades → jump to 2S.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 10, shape);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void CueBid_12PlusHcp()
    {
        // Partner doubled 1H. 14 HCP → cue bid 2H.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 14, shape);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void NTResponse_WithStopper()
    {
        // Partner doubled 1H. 8 HCP, balanced, stopper in hearts → 1NT.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 8, shape, stoppers);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void BackwardInference_CueBid_Shows12Plus()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 14, shape);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);
        Assert.That(info, Is.Not.Null);
    }

    [Test]
    public void BackwardInference_MinimumBid_Shows0to8()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvanceContext(Bid.SuitBid(1, Suit.Hearts), 5, shape);

        var rule = new AdvanceAfterTakeoutDoubleRule();
        var info = rule.GetConstraintForBid(Bid.SuitBid(1, Suit.Spades), ctx);
        Assert.That(info, Is.Not.Null);
    }
}
