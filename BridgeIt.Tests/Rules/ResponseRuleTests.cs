using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class ResponseRuleTests
{
    // =============================================
    // Helpers
    // =============================================

    private static DecisionContext CreateResponseTo1NTContext(
        int hcp, Dictionary<Suit, int> shape, Suit longestSuit = Suit.Clubs)
    {
        // North opens 1NT, East passes — South responds
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = longestSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var knowledge = new PartnershipKnowledge
        {
            PartnershipBiddingState = PartnershipBiddingState.ConstructiveSearch,
            PartnerHcpMin = 12,
            PartnerHcpMax = 14,
            PartnerIsBalanced = true
        };

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, knowledge);
    }

    private static DecisionContext CreateWrongStateContext()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 10,
            Shape = new Dictionary<Suit, int>
                { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } },
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        // Wrong state — not ConstructiveSearch after 1NT
        var knowledge = new PartnershipKnowledge
        {
            PartnershipBiddingState = PartnershipBiddingState.Unknown
        };

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, knowledge);
    }

    // =============================================
    // AcolRedSuitTransfer — CouldMakeBid
    // =============================================

    [Test]
    public void Transfer_CouldMakeBid_TrueWith5Hearts()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Hearts);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldMakeBid_TrueWith5Spades()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Spades);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWithNo5CardMajor()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWhenWrongState()
    {
        var rule = new AcolRedSuitTransfer();
        var ctx = CreateWrongStateContext();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWhenNotRound1()
    {
        var rule = new AcolRedSuitTransfer();
        // Build a context where BiddingRound != 1
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds))); // South already bid
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, Suit.Hearts))); // Opener completes
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        // South's second turn — BiddingRound = 2

        var handEval = new HandEvaluation
        {
            Hcp = 8,
            Shape = new Dictionary<Suit, int>
                { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } },
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var knowledge = new PartnershipKnowledge
        {
            PartnershipBiddingState = PartnershipBiddingState.ConstructiveSearch
        };
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, knowledge);

        Assert.That(rule.CouldMakeBid(decCtx), Is.False);
    }

    // =============================================
    // AcolRedSuitTransfer — Apply
    // =============================================

    [Test]
    public void Transfer_Apply_5Hearts_Bids2D()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Hearts);

        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void Transfer_Apply_5Spades_Bids2H()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Spades);

        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void Transfer_Apply_BothMajors5Plus_TransfersHearts()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 2 }, { Suit.Clubs, 1 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Hearts);

        // Hearts checked first in the rule, so should bid 2D (transfer to hearts)
        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    // =============================================
    // AcolRedSuitTransfer — CouldExplainBid (Backward)
    // =============================================

    [Test]
    public void Transfer_CouldExplainBid_TrueFor2D_InCorrectContext()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldExplainBid_TrueFor2H_InCorrectContext()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldExplainBid_FalseFor2C()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldExplainBid_FalseFor2S()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.False);
    }

    // =============================================
    // AcolRedSuitTransfer — GetConstraintForBid
    // =============================================

    [Test]
    public void Transfer_GetConstraintForBid_2D_ConstrainsHearts5to11()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Diamonds), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint, Is.Not.Null);
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
        Assert.That(suitConstraint.MinLen, Is.EqualTo(5));
        Assert.That(suitConstraint.MaxLen, Is.EqualTo(11));
    }

    [Test]
    public void Transfer_GetConstraintForBid_2H_ConstrainsSpades5to11()
    {
        var rule = new AcolRedSuitTransfer();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        var composite = info!.Constraint as CompositeConstraint;
        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Spades));
        Assert.That(suitConstraint.MinLen, Is.EqualTo(5));
    }
}
