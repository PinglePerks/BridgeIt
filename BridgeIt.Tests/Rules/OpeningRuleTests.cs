using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Openings;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Analysis.Hands;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class OpeningRuleTests
{
    // =============================================
    // Helpers
    // =============================================

    private static DecisionContext CreateOpeningContext(
        int hcp, bool balanced, Suit longestSuit, Dictionary<Suit, int>? shape = null)
    {
        var defaultShape = shape ?? new Dictionary<Suit, int>
        {
            { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 }
        };

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            IsBalanced = balanced,
            Shape = defaultShape,
            LongestAndStrongest = longestSuit,
            Losers = 7
        };

        var history = new AuctionHistory(Seat.North);
        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, new TableKnowledge(Seat.North));
    }

    private static DecisionContext CreateNonOpeningContext(int hcp, bool balanced)
    {
        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            IsBalanced = balanced,
            Shape = new Dictionary<Suit, int>
                { { Suit.Spades, 4 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } },
            LongestAndStrongest = Suit.Spades,
            Losers = 7
        };

        // Someone has already bid — not an opening context
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var aucEval = AuctionEvaluator.Evaluate(history);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, new TableKnowledge(Seat.South));
    }

    // =============================================
    // Acol1NTOpeningRule
    // =============================================

    [Test]
    [TestCase(12, true, Description = "Min HCP balanced")]
    [TestCase(13, true, Description = "Mid HCP balanced")]
    [TestCase(14, true, Description = "Max HCP balanced")]
    public void Acol1NT_CouldMakeBid_TrueForBalanced12to14(int hcp, bool expected)
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(hcp, balanced: true, Suit.Spades);
        Assert.That(rule.CouldMakeBid(ctx), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(11, Description = "Below min")]
    [TestCase(15, Description = "Above max")]
    public void Acol1NT_CouldMakeBid_FalseForOutOfRangeHcp(int hcp)
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(hcp, balanced: true, Suit.Spades);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Acol1NT_CouldMakeBid_FalseForUnbalanced()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(13, balanced: false, Suit.Hearts);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Acol1NT_CouldMakeBid_FalseWhenNotOpening()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateNonOpeningContext(13, balanced: true);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Acol1NT_Apply_Returns1NT()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(13, balanced: true, Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void Acol1NT_CouldExplainBid_TrueFor1NT_WhenNoBids()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs); // Hand doesn't matter for backward
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.True);
    }

    [Test]
    public void Acol1NT_CouldExplainBid_FalseFor2NT()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void Acol1NT_CouldExplainBid_FalseWhenNotOpening()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateNonOpeningContext(0, false);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    [Test]
    public void Acol1NT_GetConstraintForBid_ContainsHcpAndBalanced()
    {
        var rule = new Acol1NTOpeningRule();
        var ctx = CreateOpeningContext(13, true, Suit.Spades);
        var info = rule.GetConstraintForBid(Bid.NoTrumpsBid(1), ctx);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Constraint, Is.TypeOf<CompositeConstraint>());

        var composite = (CompositeConstraint)info.Constraint!;
        Assert.That(composite.Constraints, Has.Some.TypeOf<HcpConstraint>());
        Assert.That(composite.Constraints, Has.Some.TypeOf<BalancedConstraint>());
    }

    // =============================================
    // Acol1SuitOpeningRule
    // =============================================

    [Test]
    [TestCase(12, Description = "Min HCP")]
    [TestCase(15, Description = "Mid HCP")]
    [TestCase(19, Description = "Max HCP")]
    public void Acol1Suit_CouldMakeBid_TrueFor12to19(int hcp)
    {
        var rule = new Acol1SuitOpeningRule();
        var ctx = CreateOpeningContext(hcp, balanced: false, Suit.Hearts);
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    [TestCase(11, Description = "Below min")]
    [TestCase(20, Description = "Above max")]
    public void Acol1Suit_CouldMakeBid_FalseForOutOfRange(int hcp)
    {
        var rule = new Acol1SuitOpeningRule();
        var ctx = CreateOpeningContext(hcp, balanced: false, Suit.Hearts);
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Acol1Suit_Apply_BidsLongestSuit()
    {
        var rule = new Acol1SuitOpeningRule();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 2 }, { Suit.Hearts, 5 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateOpeningContext(14, balanced: false, Suit.Hearts, shape);

        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void Acol1Suit_Apply_BidsMinorWhenNoMajor()
    {
        var rule = new Acol1SuitOpeningRule();
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 3 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 4 } };
        var ctx = CreateOpeningContext(14, balanced: false, Suit.Diamonds, shape);

        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Diamonds)));
    }

    [Test]
    public void Acol1Suit_CouldExplainBid_TrueForAny1LevelSuit()
    {
        var rule = new Acol1SuitOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Clubs), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Hearts), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Spades), ctx), Is.True);
    }

    [Test]
    public void Acol1Suit_CouldExplainBid_FalseFor1NT()
    {
        var rule = new Acol1SuitOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    [Test]
    public void Acol1Suit_CouldExplainBid_FalseFor2Level()
    {
        var rule = new Acol1SuitOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    // =============================================
    // AcolStrongOpening
    // =============================================

    [Test]
    [TestCase(23, true, Description = "Balanced 23 HCP")]
    [TestCase(25, true, Description = "Balanced 25 HCP")]
    [TestCase(22, false, Description = "Balanced 22 HCP - too low, opens 2NT")]
    [TestCase(19, false, Description = "Balanced 19 HCP - too low")]
    public void AcolStrong_CouldMakeBid_Balanced(int hcp, bool expected)
    {
        var rule = new AcolStrongOpening();
        var ctx = CreateOpeningContext(hcp, balanced: true, Suit.Spades);
        Assert.That(rule.CouldMakeBid(ctx), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(20, true, Description = "Unbalanced 20 HCP")]
    [TestCase(25, true, Description = "Unbalanced 25 HCP")]
    [TestCase(19, false, Description = "Unbalanced 19 HCP - too low")]
    public void AcolStrong_CouldMakeBid_Unbalanced(int hcp, bool expected)
    {
        var rule = new AcolStrongOpening();
        var ctx = CreateOpeningContext(hcp, balanced: false, Suit.Spades);
        Assert.That(rule.CouldMakeBid(ctx), Is.EqualTo(expected));
    }

    [Test]
    public void AcolStrong_Apply_Returns2C()
    {
        var rule = new AcolStrongOpening();
        var ctx = CreateOpeningContext(23, balanced: true, Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Clubs)));
    }

    [Test]
    public void AcolStrong_CouldExplainBid_TrueFor2C_WhenNoBids()
    {
        var rule = new AcolStrongOpening();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.True);
    }

    [Test]
    public void AcolStrong_CouldExplainBid_FalseForOtherBids()
    {
        var rule = new AcolStrongOpening();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    // =============================================
    // WeakOpeningRule
    // =============================================

    [Test]
    public void WeakOpening_CouldMakeBid_TrueForWeakLongSuit()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 1 }, { Suit.Hearts, 6 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateOpeningContext(7, balanced: false, Suit.Hearts, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void WeakOpening_CouldMakeBid_FalseForReservedBid()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 1 }, { Suit.Hearts, 2 }, { Suit.Diamonds, 4 }, { Suit.Clubs, 6 } };
        var ctx = CreateOpeningContext(7, balanced: false, Suit.Clubs, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void WeakOpening_CouldMakeBid_FalseForTooManyHcp()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 1 }, { Suit.Hearts, 6 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 3 } };
        var ctx = CreateOpeningContext(12, balanced: false, Suit.Hearts, shape);

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void WeakOpening_CouldExplainBid_TrueFor2LevelSuit_NotReserved()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(3, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void WeakOpening_CouldExplainBid_FalseForReserved2C()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.False);
    }

    [Test]
    public void WeakOpening_CouldExplainBid_FalseFor1Level()
    {
        var rule = new WeakOpeningRule([Bid.SuitBid(2, Suit.Clubs)]);
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);

        Assert.That(rule.CouldExplainBid(Bid.SuitBid(1, Suit.Hearts), ctx), Is.False);
    }

    // =============================================
    // Acol2NTOpeningRule
    // =============================================

    [Test]
    [TestCase(20, true)]
    [TestCase(22, true)]
    [TestCase(19, false)]
    [TestCase(23, false)]
    public void Acol2NT_CouldMakeBid_ChecksRange(int hcp, bool expected)
    {
        var rule = new Acol2NTOpeningRule();
        var ctx = CreateOpeningContext(hcp, balanced: true, Suit.Spades);
        Assert.That(rule.CouldMakeBid(ctx), Is.EqualTo(expected));
    }

    [Test]
    public void Acol2NT_Apply_Returns2NT()
    {
        var rule = new Acol2NTOpeningRule();
        var ctx = CreateOpeningContext(21, balanced: true, Suit.Spades);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Acol2NT_CouldExplainBid_TrueFor2NT()
    {
        var rule = new Acol2NTOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void Acol2NT_CouldExplainBid_FalseFor1NT()
    {
        var rule = new Acol2NTOpeningRule();
        var ctx = CreateOpeningContext(0, false, Suit.Clubs);
        Assert.That(rule.CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }
}
