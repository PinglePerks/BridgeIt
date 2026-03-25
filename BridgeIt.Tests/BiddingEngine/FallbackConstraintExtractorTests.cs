using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.BiddingEngine;

[TestFixture]
public class FallbackConstraintExtractorTests
{
    [Test]
    public void Pass_ReturnsNull()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.Pass());
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Redouble_ReturnsNull()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.Redouble());
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SuitBid_Level1_ReturnsNonNullWithHcpAndSuitConstraints()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(1, Suit.Hearts));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));

        var composite = result.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);
        Assert.That(composite!.Constraints, Has.Count.EqualTo(2));
    }

    [Test]
    public void SuitBid_Level1_HcpMinIs6()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(1, Suit.Spades));

        var composite = (CompositeConstraint)result!.Constraint!;
        var hcp = composite.Constraints.OfType<HcpConstraint>().Single();
        Assert.That(hcp.Min, Is.EqualTo(6));
    }

    [Test]
    public void SuitBid_Level1_SuitMinLengthIs3()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(1, Suit.Clubs));

        var composite = (CompositeConstraint)result!.Constraint!;
        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().Single();
        Assert.That(suitLen.MinLen, Is.EqualTo(3));
        Assert.That(suitLen.Suit, Is.EqualTo(Suit.Clubs));
    }

    [Test]
    public void SuitBid_Level2_SuitMinLengthIs5()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(2, Suit.Hearts));

        var composite = (CompositeConstraint)result!.Constraint!;
        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().Single();
        Assert.That(suitLen.MinLen, Is.EqualTo(5));
    }

    [Test]
    public void SuitBid_Level4_SuitMinLengthIs7()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(4, Suit.Spades));

        var composite = (CompositeConstraint)result!.Constraint!;
        var suitLen = composite.Constraints.OfType<SuitLengthConstraint>().Single();
        Assert.That(suitLen.MinLen, Is.EqualTo(7));
    }

    [Test]
    public void NtBid_Level1_ReturnsNonNullWithHcpAndBalancedConstraints()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.NoTrumpsBid(1));

        Assert.That(result, Is.Not.Null);
        var composite = result!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);
        Assert.That(composite!.Constraints.OfType<BalancedConstraint>().Any(), Is.True);
    }

    [Test]
    public void NtBid_Level1_HcpMinIs10()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.NoTrumpsBid(1));

        var composite = (CompositeConstraint)result!.Constraint!;
        var hcp = composite.Constraints.OfType<HcpConstraint>().Single();
        Assert.That(hcp.Min, Is.EqualTo(10));
    }

    [Test]
    public void NtBid_Level3_HcpMinIs22()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.NoTrumpsBid(3));

        var composite = (CompositeConstraint)result!.Constraint!;
        var hcp = composite.Constraints.OfType<HcpConstraint>().Single();
        Assert.That(hcp.Min, Is.EqualTo(22));
    }

    [Test]
    public void Double_ReturnsHcpMinimumOf8()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.Double());

        Assert.That(result, Is.Not.Null);
        var hcp = result!.Constraint as HcpConstraint;
        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(8));
    }

    [Test]
    public void SuitBid_PartnershipStateIsUnknown()
    {
        var result = FallbackConstraintExtractor.Extract(Bid.SuitBid(1, Suit.Hearts));
        Assert.That(result!.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.Unknown));
    }
}
