using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
// For HandEvaluation
// For AuctionEvaluation, PartnershipKnowledge, AuctionHistory

// For Hand, Seat, Vulnerability

namespace BridgeIt.Tests.BiddingEngine.Constraints;

[TestFixture]
public class HcpConstraintTests
{
    // --- 1. Testing Range Parsing Logic (Constructor) ---

    [Test]
    public void Constructor_ParsesRangeString_Correctly()
    {
        var constraint = new HcpConstraint("12-14");
        Assert.That(constraint.Min, Is.EqualTo(12));
        Assert.That(constraint.Max, Is.EqualTo(14));
    }

    [Test]
    public void Constructor_ParsesGreaterOrEqual_Correctly()
    {
        var constraint = new HcpConstraint(">=12");
        Assert.That(constraint.Min, Is.EqualTo(12));
        Assert.That(constraint.Max, Is.EqualTo(40)); // Default max
    }

    [Test]
    public void Constructor_ParsesExactValue_Correctly()
    {
        var constraint = new HcpConstraint("15");
        Assert.That(constraint.Min, Is.EqualTo(15));
        Assert.That(constraint.Max, Is.EqualTo(15));
    }

    // --- 2. Testing Evaluation Logic (IsMet) ---

    [Test]
    [TestCase("12-14", 11, false, Description = "Below Min")]
    [TestCase("12-14", 12, true,  Description = "Exact Min")]
    [TestCase("12-14", 13, true,  Description = "In Middle")]
    [TestCase("12-14", 14, true,  Description = "Exact Max")]
    [TestCase("12-14", 15, false, Description = "Above Max")]
    public void IsMet_EvaluatesStandardRange_Correctly(string range, int handHcp, bool expectedResult)
    {
        // Arrange
        var constraint = new HcpConstraint(range);
        var context = CreateContextWithHcp(handHcp);

        // Act
        var result = constraint.IsMet(context);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase(">=12", 11, false)]
    [TestCase(">=12", 12, true)]
    [TestCase(">=12", 25, true)]
    public void IsMet_EvaluatesOpenEndedRange_Correctly(string range, int handHcp, bool expectedResult)
    {
        var constraint = new HcpConstraint(range);
        var context = CreateContextWithHcp(handHcp);
        Assert.That(constraint.IsMet(context), Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase("15", 14, false)]
    [TestCase("15", 15, true)]
    [TestCase("15", 16, false)]
    public void IsMet_EvaluatesExactPoint_Correctly(string range, int handHcp, bool expectedResult)
    {
        var constraint = new HcpConstraint(range);
        var context = CreateContextWithHcp(handHcp);
        Assert.That(constraint.IsMet(context), Is.EqualTo(expectedResult));
    }

    // --- Helper to inject specific HCP into the Context ---
    private BiddingContext CreateContextWithHcp(int hcp)
    {
        // Create the specific evaluation data we want to test
        var handEvaluation = new HandEvaluation
        {
            Hcp = hcp,
            Losers = 0, // Irrelevant for this test
            Shape = new Dictionary<Suit, int>(), // Irrelevant
            IsBalanced = false // Irrelevant
        };

        // Fill the rest with Dummies / Empty objects
        // Since HcpConstraint ONLY looks at HandEvaluation.Hcp, 
        // we can pass nulls or empty objects for everything else.
        
        // Note: Creating a dummy Hand might require an internal list if constructor verifies it,
        // but here we assume it's fine to pass a minimal object.
        var dummyHand = new Hand(new List<Card>()); 
        var dummyHistory = new AuctionHistory(new List<BiddingDecision>(), Seat.North);
        
        return new BiddingContext(
            dummyHand,
            dummyHistory,
            Seat.North,
            Vulnerability.None,
            handEvaluation, // <--- The important part
            new PartnershipKnowledge(),
            new AuctionEvaluation()
        );
    }
}