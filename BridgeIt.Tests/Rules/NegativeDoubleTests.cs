using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Competitive;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class NegativeDoubleTests
{
    /// <summary>
    /// N (partner) opens 1D, E overcalls 1H, S (responder) to bid.
    /// </summary>
    private static DecisionContext CreateNegativeDoubleContext(
        Bid openingBid, Bid overcall, int hcp, Dictionary<Suit, int> shape)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, openingBid));
        history.Add(new AuctionBid(Seat.East, overcall));

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
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 19;
        if (openingBid.Suit.HasValue)
            tableKnowledge.Partner.MinShape[openingBid.Suit.Value] = 4;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    [Test]
    public void NegativeDouble_ShowsUnbidMajors()
    {
        // N opens 1D, E overcalls 1H. S has 8 HCP, 4 spades → negative double.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Diamonds), Bid.SuitBid(1, Suit.Hearts), 8, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.Double()));
    }

    [Test]
    public void NegativeDouble_TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Diamonds), Bid.SuitBid(1, Suit.Hearts), 4, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NegativeDouble_NoUnbidMajor_DoesNotApply()
    {
        // N opens 1C, E overcalls 1S. Unbid majors = hearts only. S has only 3 hearts.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Clubs), Bid.SuitBid(1, Suit.Spades), 8, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NegativeDouble_4InUnbidMajor_Applies()
    {
        // N opens 1C, E overcalls 1S. S has 4 hearts → double.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Clubs), Bid.SuitBid(1, Suit.Spades), 8, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NegativeDouble_OvercallAboveMaxLevel_DoesNotApply()
    {
        // maxLevel = 2S. Opponent overcalls at 3C → above limit.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Diamonds), Bid.SuitBid(3, Suit.Clubs), 10, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NegativeDouble_OvercallAt2Level_WithinLimit()
    {
        // N opens 1C, E overcalls 2D (within 2S limit). S has 4-4 majors.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 2 }, { Suit.Clubs, 3 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Clubs), Bid.SuitBid(2, Suit.Diamonds), 9, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NegativeDouble_IsAlertable()
    {
        var rule = new NegativeDoubleRule();
        Assert.That(rule.IsAlertable, Is.True);
    }

    [Test]
    public void NegativeDouble_BackwardInference()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateNegativeDoubleContext(
            Bid.SuitBid(1, Suit.Diamonds), Bid.SuitBid(1, Suit.Hearts), 8, shape);

        var rule = new NegativeDoubleRule("2S");
        Assert.That(rule.CouldExplainBid(Bid.Double(), ctx), Is.True);
        var info = rule.GetConstraintForBid(Bid.Double(), ctx);
        Assert.That(info, Is.Not.Null);
    }
}
