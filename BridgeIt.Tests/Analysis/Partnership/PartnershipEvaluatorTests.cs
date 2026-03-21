using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Partnership;

[TestFixture]
public class PartnershipEvaluatorTests
{
    [Test]
    public void AnalyzeKnowledge_EmptyList_ReturnsDefaults()
    {
        var result = PartnershipEvaluator.AnalyzeKnowledge(new List<BidInformation>());

        Assert.Multiple(() =>
        {
            Assert.That(result.PartnerHcpMin, Is.EqualTo(0));
            Assert.That(result.PartnerHcpMax, Is.EqualTo(40));
            Assert.That(result.PartnerIsBalanced, Is.False);
            Assert.That(result.PartnerMinShape[Suit.Hearts], Is.EqualTo(0));
            Assert.That(result.PartnerMaxShape[Suit.Hearts], Is.EqualTo(13));
        });
    }

    [Test]
    public void AnalyzeKnowledge_HcpConstraint_NarrowsRange()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), new HcpConstraint(12, 14), PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.That(result.PartnerHcpMin, Is.EqualTo(12));
        Assert.That(result.PartnerHcpMax, Is.EqualTo(14));
    }

    [Test]
    public void AnalyzeKnowledge_BalancedConstraint_SetsBalancedAndBoundsShape()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), new BalancedConstraint(), PartnershipBiddingState.ConstructiveSearch)
        };

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.PartnerIsBalanced, Is.True);
            Assert.That(result.PartnerMinShape[Suit.Spades], Is.EqualTo(2));
            Assert.That(result.PartnerMaxShape[Suit.Spades], Is.EqualTo(5));
            Assert.That(result.PartnerMinShape[Suit.Clubs], Is.EqualTo(2));
            Assert.That(result.PartnerMaxShape[Suit.Clubs], Is.EqualTo(5));
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

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.That(result.PartnerMinShape[Suit.Hearts], Is.EqualTo(4));
        Assert.That(result.PartnerMaxShape[Suit.Hearts], Is.EqualTo(11));
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

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            Assert.That(result.PartnerHcpMin, Is.EqualTo(12));
            Assert.That(result.PartnerHcpMax, Is.EqualTo(14));
            Assert.That(result.PartnerIsBalanced, Is.True);
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

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);

        Assert.Multiple(() =>
        {
            // HCP narrowed by second bid
            Assert.That(result.PartnerHcpMin, Is.EqualTo(15));
            Assert.That(result.PartnerHcpMax, Is.EqualTo(19));
            // Both suits now have minimums
            Assert.That(result.PartnerMinShape[Suit.Hearts], Is.EqualTo(4));
            Assert.That(result.PartnerMinShape[Suit.Spades], Is.EqualTo(4));
        });
    }

    [Test]
    public void AnalyzeKnowledge_SetsPartnershipBiddingState_FromLastBid()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.NoTrumpsBid(1), null, PartnershipBiddingState.ConstructiveSearch),
            new(Bid.SuitBid(2, Suit.Hearts), null, PartnershipBiddingState.FitEstablished)
        };

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);
        Assert.That(result.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.FitEstablished));
    }

    [Test]
    public void AnalyzeKnowledge_NullConstraint_DoesNotThrow()
    {
        var bidInfos = new List<BidInformation>
        {
            new(Bid.Pass(), null, PartnershipBiddingState.Unknown)
        };

        var result = PartnershipEvaluator.AnalyzeKnowledge(bidInfos);
        Assert.That(result.PartnerHcpMin, Is.EqualTo(0));
        Assert.That(result.PartnerHcpMax, Is.EqualTo(40));
    }
}
