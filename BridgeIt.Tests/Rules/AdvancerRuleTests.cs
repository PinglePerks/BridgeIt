using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class AdvancerRuleTests
{
    /// <summary>
    /// N opens 1H, E (partner) overcalls 1S, S passes, W (advancer) to bid.
    /// </summary>
    private static DecisionContext CreateAdvancerContext(
        Bid openingBid, Bid partnerOvercall, int hcp, Dictionary<Suit, int> shape,
        Dictionary<Suit, StopperQuality>? stoppers = null)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, openingBid));
        history.Add(new AuctionBid(Seat.East, partnerOvercall));
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
        // Partner overcalled — infer 5+ in their suit, 8-15 HCP
        tableKnowledge.Partner.HcpMin = 8;
        tableKnowledge.Partner.HcpMax = 15;
        if (partnerOvercall.Suit.HasValue)
            tableKnowledge.Partner.MinShape[partnerOvercall.Suit.Value] = 5;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.West, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // ═══════════════════════════════════════════════════════════════
    // RAISE OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RaiseOvercall_SimpleRaise_3Support_8Hcp()
    {
        // Partner overcalled 1S. 3 spades, 9 HCP → raise to 2S.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 9, shape);

        var rule = new RaiseOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void RaiseOvercall_JumpRaise_4Support_Weak()
    {
        // Partner overcalled 1S. 4 spades, 5 HCP → preemptive jump to 3S.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 5, shape);

        var rule = new RaiseOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        // Cheapest for spades is 2S. Jump = 3S.
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void RaiseOvercall_NoSupport_DoesNotApply()
    {
        // Partner overcalled 1S. Only 2 spades, 9 HCP → no raise.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 9, shape);

        var rule = new RaiseOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseOvercall_TooManyHcp_ForSimpleRaise()
    {
        // 3 support, 13 HCP — too strong for simple raise (8-11).
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 13, shape);

        var rule = new RaiseOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // NEW SUIT OVER OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NewSuit_5CardSuit_8Hcp()
    {
        // Partner overcalled 1S. 5 diamonds, 10 HCP → bid 2D.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 5 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 10, shape);

        var rule = new NewSuitOverOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void NewSuit_TooFewHcp_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 5 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 6, shape);

        var rule = new NewSuitOverOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NewSuit_Only4Cards_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 10, shape);

        var rule = new NewSuitOverOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // NT RESPONSE TO OVERCALL
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NTResponse_BalancedWithStopper_NoFit()
    {
        // Partner overcalled 1S. 2 spades (no fit), balanced, stopper in hearts, 10 HCP.
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 10, shape, stoppers);

        var rule = new NTResponseToOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTResponse_HasFit_DoesNotApply()
    {
        // 3+ support in partner's suit → should raise instead
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var stoppers = new Dictionary<Suit, StopperQuality> { { Suit.Hearts, StopperQuality.Full } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 10, shape, stoppers);

        var rule = new NTResponseToOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTResponse_NoStopper_DoesNotApply()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateAdvancerContext(
            Bid.SuitBid(1, Suit.Hearts), Bid.SuitBid(1, Suit.Spades), 10, shape);

        var rule = new NTResponseToOvercallRule();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }
}
