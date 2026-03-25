using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class PlayerKnowledgeEvaluatorTests
{
    [Test]
    public void AnalyzeKnowledge_EmptyList_ReturnsDefaults()
    {
        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(new List<BidInformation>());

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMin, Is.EqualTo(0));
            Assert.That(result.HcpMax, Is.EqualTo(37));
            Assert.That(result.IsBalanced, Is.False);
            Assert.That(result.MinShape[Suit.Hearts], Is.EqualTo(0));
            Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(13));
        });
    }

    [Test]
    public void AnalyzeKnowledge_HcpConstraint_NarrowsRange()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), new HcpConstraint(12, 14), PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.That(result.HcpMin, Is.EqualTo(12));
        Assert.That(result.HcpMax, Is.EqualTo(14));
    }

    [Test]
    public void AnalyzeKnowledge_BalancedConstraint_SetsBalancedAndBoundsShape()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), new BalancedConstraint(), PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsBalanced, Is.True);
            Assert.That(result.MinShape[Suit.Spades], Is.EqualTo(2));
            Assert.That(result.MaxShape[Suit.Spades], Is.EqualTo(5));
            Assert.That(result.MinShape[Suit.Clubs], Is.EqualTo(2));
            Assert.That(result.MaxShape[Suit.Clubs], Is.EqualTo(5));
        });
    }

    [Test]
    public void AnalyzeKnowledge_SuitLengthConstraint_UpdatesSuitBounds()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.SuitBid(1, Suit.Hearts),
                new SuitLengthConstraint(Suit.Hearts, 4, 11),
                PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.That(result.MinShape[Suit.Hearts], Is.EqualTo(4));
        Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(11));
    }

    [Test]
    public void AnalyzeKnowledge_CompositeConstraint_AppliesBoth()
    {
        var composite = new CompositeConstraint();
        composite.Add(new HcpConstraint(12, 14));
        composite.Add(new BalancedConstraint());

        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), composite, PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMin, Is.EqualTo(12));
            Assert.That(result.HcpMax, Is.EqualTo(14));
            Assert.That(result.IsBalanced, Is.True);
        });
    }

    [Test]
    public void AnalyzeKnowledge_MultipleBids_NarrowProgressively()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.SuitBid(1, Suit.Hearts),
                new CompositeConstraint
                {
                    Constraints = { new HcpConstraint(12, 19), new SuitLengthConstraint(Suit.Hearts, 4, 13) }
                },
                PartnershipBiddingState.ConstructiveSearch),
            new(Bid.SuitBid(2, Suit.Spades),
                new CompositeConstraint
                {
                    Constraints = { new HcpConstraint(15, 19), new SuitLengthConstraint(Suit.Spades, 4, 13) }
                },
                PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMin, Is.EqualTo(15));
            Assert.That(result.HcpMax, Is.EqualTo(19));
            Assert.That(result.MinShape[Suit.Hearts], Is.EqualTo(4));
            Assert.That(result.MinShape[Suit.Spades], Is.EqualTo(4));
        });
    }

    [Test]
    public void AnalyzeKnowledge_NullConstraint_DoesNotThrow()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.Pass(), null, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);
        Assert.That(result.HcpMin, Is.EqualTo(0));
        Assert.That(result.HcpMax, Is.EqualTo(37));
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Negated constraint tests
    // ══════════════════════════════════════════════════════════════════════════

    [Test]
    public void NegatedSingleHcp_CapsHcpMax()
    {
        // A single-component negation: NOT(HCP >= 12)
        // With no prior positive knowledge, the one component is unsatisfied → negated.
        var neg = new NegatedCompositeConstraint();
        neg.Add(new HcpConstraint(12, 40));

        var bidInfos = new List<BidInformation>
        {
            new(Bid.Pass(), neg, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);
        Assert.That(result.HcpMax, Is.EqualTo(11));
    }

    [Test]
    public void NegatedHcpAndSuit_WhenHcpAlreadyKnown_InfersSuitMax()
    {
        // Player opened 1♠ (showing 12+ HCP), then partner raised hearts,
        // and this player passed when a raise rule (6+ HCP, 4+ hearts) was applicable.
        // Since HCP >= 6 is already known (they have 12+), the negation
        // NOT(HCP >= 6 AND hearts >= 4) forces hearts < 4.
        var positiveFromOpening = new CompositeConstraint
        {
            Constraints = { new HcpConstraint(12, 19) }
        };

        var negFromPass = new NegatedCompositeConstraint();
        negFromPass.Add(new HcpConstraint(6, 40));
        negFromPass.Add(new SuitLengthConstraint(Suit.Hearts, 4, 13));

        var bidInfos = new List<BidInformation>
        {
            new(Bid.SuitBid(1, Suit.Spades), positiveFromOpening, PartnershipBiddingState.ConstructiveSearch),
            new(Bid.Pass(), negFromPass, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMin, Is.EqualTo(12));
            Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(3));
        });
    }

    [Test]
    public void NegatedHcpAndSuit_WhenNeitherKnown_NoInference()
    {
        // NOT(HCP >= 6 AND hearts >= 4) with no prior knowledge.
        // Two unsatisfied components → can't determine which failed → no inference.
        var neg = new NegatedCompositeConstraint();
        neg.Add(new HcpConstraint(6, 40));
        neg.Add(new SuitLengthConstraint(Suit.Hearts, 4, 13));

        var bidInfos = new List<BidInformation>
        {
            new(Bid.Pass(), neg, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            // No inference — defaults unchanged
            Assert.That(result.HcpMax, Is.EqualTo(37));
            Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(13));
        });
    }

    [Test]
    public void NegatedHcpAndSuit_WhenSuitAlreadyKnown_InfersHcpMax()
    {
        // Player showed 5+ hearts from a transfer, then passed when a rule
        // needing (6+ HCP AND 4+ hearts) was applicable.
        // Hearts >= 4 is already satisfied (they have 5+), so HCP < 6 is forced.
        var positiveFromTransfer = new CompositeConstraint
        {
            Constraints = { new SuitLengthConstraint(Suit.Hearts, 5, 13) }
        };

        var negFromPass = new NegatedCompositeConstraint();
        negFromPass.Add(new HcpConstraint(6, 40));
        negFromPass.Add(new SuitLengthConstraint(Suit.Hearts, 4, 13));

        var bidInfos = new List<BidInformation>
        {
            new(Bid.SuitBid(2, Suit.Hearts), positiveFromTransfer, PartnershipBiddingState.ConstructiveSearch),
            new(Bid.Pass(), negFromPass, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMax, Is.EqualTo(5));
            Assert.That(result.MinShape[Suit.Hearts], Is.EqualTo(5));
        });
    }

    [Test]
    public void MultipleNegations_EachResolvedIndependently()
    {
        // Player has 12+ HCP (from opening). Two negations from passing:
        //   NOT(HCP >= 6 AND hearts >= 4)  → hearts < 4
        //   NOT(HCP >= 6 AND spades >= 4)  → spades < 4
        var positive = new HcpConstraint(12, 19);

        var neg1 = new NegatedCompositeConstraint();
        neg1.Add(new HcpConstraint(6, 40));
        neg1.Add(new SuitLengthConstraint(Suit.Hearts, 4, 13));

        var neg2 = new NegatedCompositeConstraint();
        neg2.Add(new HcpConstraint(6, 40));
        neg2.Add(new SuitLengthConstraint(Suit.Spades, 4, 13));

        var passComposite = new CompositeConstraint();
        passComposite.Add(neg1);
        passComposite.Add(neg2);

        var bidInfos = new List<BidInformation>
        {
            new(Bid.SuitBid(1, Suit.Diamonds), positive, PartnershipBiddingState.ConstructiveSearch),
            new(Bid.Pass(), passComposite, PartnershipBiddingState.Unknown)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(3));
            Assert.That(result.MaxShape[Suit.Spades], Is.EqualTo(3));
            Assert.That(result.HcpMin, Is.EqualTo(12));
        });
    }

    [Test]
    public void NegatedConstraint_OrderIndependent_PositiveBidAfterPass()
    {
        // Even if the positive bid info comes AFTER the negated constraint
        // in the list, two-pass processing ensures correct resolution.
        var negFromPass = new NegatedCompositeConstraint();
        negFromPass.Add(new HcpConstraint(6, 40));
        negFromPass.Add(new SuitLengthConstraint(Suit.Hearts, 4, 13));

        var positiveFromBid = new HcpConstraint(8, 15);

        var bidInfos = new List<BidInformation>
        {
            new(Bid.Pass(), negFromPass, PartnershipBiddingState.Unknown),
            new(Bid.SuitBid(1, Suit.Spades), positiveFromBid, PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.HcpMin, Is.EqualTo(8));
            Assert.That(result.MaxShape[Suit.Hearts], Is.EqualTo(3));
        });
    }
}
