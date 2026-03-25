using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Conventions;
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
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 14;
        tableKnowledge.Partner.IsBalanced = true;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
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
        var tableKnowledge = new TableKnowledge(Seat.South);

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // =============================================
    // AcolRedSuitTransfer — CouldMakeBid
    // =============================================

    [Test]
    public void Transfer_CouldMakeBid_TrueWith5Hearts()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Hearts);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldMakeBid_TrueWith5Spades()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Spades);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWithNo5CardMajor()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWhenWrongState()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var ctx = CreateWrongStateContext();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldMakeBid_FalseWhenNotRound1()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
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
        var tableKnowledge = new TableKnowledge(Seat.South);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(rule.CouldMakeBid(decCtx), Is.False);
    }

    // =============================================
    // AcolRedSuitTransfer — Apply
    // =============================================

    [Test]
    public void Transfer_Apply_5Hearts_Bids2D()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Hearts);

        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void Transfer_Apply_5Spades_Bids2H()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var ctx = CreateResponseTo1NTContext(8, shape, Suit.Spades);

        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void Transfer_Apply_BothMajors5Plus_TransfersHearts()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
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
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldExplainBid_TrueFor2H_InCorrectContext()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void Transfer_CouldExplainBid_FalseFor2C()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.False);
    }

    [Test]
    public void Transfer_CouldExplainBid_FalseFor2S()
    {
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
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
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
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
        var rule = new StandardTransfer(NTConventionContexts.After1NT);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        var composite = info!.Constraint as CompositeConstraint;
        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Spades));
        Assert.That(suitConstraint.MinLen, Is.EqualTo(5));
    }

    // =============================================
    // GetLevelVerdict
    // =============================================

    [TestCase(5, 12, 14, ExpectedResult = LevelVerdict.SignOff)]   // max = 5+14 = 19
    [TestCase(10, 12, 14, ExpectedResult = LevelVerdict.SignOff)]  // max = 10+14 = 24
    [TestCase(11, 12, 14, ExpectedResult = LevelVerdict.Invite)]   // min=23, max=25
    [TestCase(12, 12, 14, ExpectedResult = LevelVerdict.Invite)]   // min=24, max=26
    [TestCase(13, 12, 14, ExpectedResult = LevelVerdict.BidGame)]  // min = 13+12 = 25
    [TestCase(15, 12, 14, ExpectedResult = LevelVerdict.BidGame)]  // min = 15+12 = 27
    [TestCase(8, 15, 17, ExpectedResult = LevelVerdict.Invite)]    // strong NT: min=23, max=25
    [TestCase(10, 15, 17, ExpectedResult = LevelVerdict.BidGame)]  // strong NT: min=25
    public LevelVerdict GetLevelVerdict_ReturnsCorrectVerdict(
        int myHcp, int partnerMin, int partnerMax)
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = partnerMin;
        tableKnowledge.Partner.HcpMax = partnerMax;
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        var handEval = new HandEvaluation
        {
            Hcp = myHcp, Shape = shape, IsBalanced = true, Losers = 7,
            LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        return decCtx.GetLevelVerdict();
    }

    [Test]
    public void GetLevelVerdict_MinorSuitThreshold29_SignOffWhenMaxBelow()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 5 }, { Suit.Clubs, 4 } };
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 14;
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = shape, IsBalanced = false, Losers = 7,
            LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        // 13 HCP + partner 12-14: max=27 < 29 → SignOff (can't make 5-level minor)
        Assert.That(decCtx.GetLevelVerdict(gameThreshold: 29), Is.EqualTo(LevelVerdict.SignOff));
    }

    [Test]
    public void GetLevelVerdict_MinorSuitThreshold29_InviteWhenStraddling()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 5 }, { Suit.Clubs, 4 } };
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 19;
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        var handEval = new HandEvaluation
        {
            Hcp = 15, Shape = shape, IsBalanced = false, Losers = 7,
            LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        // 15 HCP + partner 12-19: min=27, max=34 → Invite (straddles 29)
        Assert.That(decCtx.GetLevelVerdict(gameThreshold: 29), Is.EqualTo(LevelVerdict.Invite));
    }

    // =============================================
    // AcolNTRaiseOver1NT — CouldMakeBid
    // =============================================

    [Test]
    public void NTRaise_CouldMakeBid_TrueForBalancedHand()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(10, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTRaise_CouldMakeBid_TrueEvenWithWeakHand()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(3, shape);

        // This rule always applies in the right context — it handles Pass too
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTRaise_CouldMakeBid_FalseWhenWrongState()
    {
        var rule = new AcolNTRaiseOver1NT();
        var ctx = CreateWrongStateContext();
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTRaise_CouldMakeBid_FalseWhenNotRound1()
    {
        var rule = new AcolNTRaiseOver1NT();
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(2)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        // Now it's North's turn (BiddingRound 2)

        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = new Dictionary<Suit, int>
                { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } },
            IsBalanced = true, Losers = 6, LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(rule.CouldMakeBid(decCtx), Is.False);
    }

    // =============================================
    // AcolNTRaiseOver1NT — Apply (uses GetLevelVerdict)
    // =============================================

    [TestCase(5, "Pass")]
    [TestCase(8, "Pass")]
    [TestCase(10, "Pass")]
    [TestCase(11, "2NT")]
    [TestCase(12, "2NT")]
    [TestCase(13, "3NT")]
    [TestCase(16, "3NT")]
    public void NTRaise_Apply_CorrectBidForHcp(int hcp, string expectedBid)
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(hcp, shape);

        var bid = rule.Apply(ctx);
        Assert.That(bid!.ToString(), Is.EqualTo(expectedBid));
    }

    // =============================================
    // AcolNTRaiseOver1NT — CouldExplainBid (Backward)
    // =============================================

    [Test]
    public void NTRaise_CouldExplainBid_TrueForPass()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.Pass(), ctx), Is.True);
    }

    [Test]
    public void NTRaise_CouldExplainBid_TrueFor2NT()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void NTRaise_CouldExplainBid_TrueFor3NT()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(3), ctx), Is.True);
    }

    [Test]
    public void NTRaise_CouldExplainBid_FalseFor1NT()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    [Test]
    public void NTRaise_CouldExplainBid_FalseForSuitBid()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    // =============================================
    // AcolNTRaiseOver1NT — GetConstraintForBid
    // =============================================

    [Test]
    public void NTRaise_GetConstraint_Pass_Hcp0to10()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.Pass(), ctx);
        Assert.That(info, Is.Not.Null);
        var hcp = info!.Constraint as HcpConstraint;
        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(0));
        Assert.That(hcp.Max, Is.EqualTo(10));
        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    [Test]
    public void NTRaise_GetConstraint_2NT_Hcp11to12_GameInvitational()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);
        Assert.That(info, Is.Not.Null);
        var hcp = info!.Constraint as HcpConstraint;
        Assert.That(hcp!.Min, Is.EqualTo(12));
        Assert.That(hcp.Max, Is.EqualTo(12));
        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.GameInvitational));
    }

    [Test]
    public void NTRaise_GetConstraint_3NT_Hcp13Plus_SignOff()
    {
        var rule = new AcolNTRaiseOver1NT();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } };
        var ctx = CreateResponseTo1NTContext(0, shape);

        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(3), ctx);
        Assert.That(info, Is.Not.Null);
        var hcp = info!.Constraint as HcpConstraint;
        Assert.That(hcp!.Min, Is.EqualTo(13));
        Assert.That(hcp.Max, Is.EqualTo(30));
        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    // =============================================
    // CompleteTransfer — CouldMakeBid
    // =============================================

    private static DecisionContext CreateCompleteTransferContext(Suit transferSuit)
    {
        // North opens 1NT, East passes, South bids transfer, West passes — North to complete
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, transferSuit)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 13,
            Shape = new Dictionary<Suit, int>
                { { Suit.Spades, 3 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 3 } },
            IsBalanced = true, Losers = 6, LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        tableKnowledge.Partner.HcpMin = 0;
        tableKnowledge.Partner.HcpMax = 40;
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    [Test]
    public void CompleteTransfer_CouldMakeBid_TrueAfter2D()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Diamonds);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void CompleteTransfer_CouldMakeBid_TrueAfter2H()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Hearts);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void CompleteTransfer_Apply_After2D_Bids2H()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Diamonds);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void CompleteTransfer_Apply_After2H_Bids2S()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Hearts);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void CompleteTransfer_CouldExplainBid_TrueFor2H_After2DTransfer()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Diamonds);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void CompleteTransfer_CouldExplainBid_FalseFor2S_After2DTransfer()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Diamonds);
        // 2D transfer should only explain 2H, not 2S
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.False);
    }

    [Test]
    public void CompleteTransfer_CouldExplainBid_TrueFor2S_After2HTransfer()
    {
        var rule = new CompleteTransfer(NTConventionContexts.After1NT);
        var ctx = CreateCompleteTransferContext(Suit.Hearts);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.True);
    }
}
