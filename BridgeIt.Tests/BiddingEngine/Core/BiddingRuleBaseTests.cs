using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.BiddingEngine.Core;

[TestFixture]
public class BiddingRuleBaseTests
{
    private class TestableBiddingRule : BiddingRuleBase
    {
        public override string Name => "Testable Rule";
        public override int Priority => 0;
        public override bool IsApplicable(BiddingContext ctx) => false;
        public override (IBidConstraint?, string?) GetConstraintForBid(Bid bid, BiddingContext ctx)
        {
            throw new NotImplementedException();
        }

        public override BiddingDecision? Apply(BiddingContext ctx) => null;

        // Expose protected methods as public for testing
        public new int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
            => base.GetNextSuitBidLevel(suit, currentContract);

        public new int GetNextNtBidLevel(Bid? currentContract)
            => base.GetNextNtBidLevel(currentContract);

        public new int Hcp(Hand hand) => base.Hcp(hand);

        public new bool IsBalanced(Hand hand) => base.IsBalanced(hand);

        public new Suit LongestAndStrongest(Hand hand) 
            => base.LongestAndStrongest(hand);

        public new bool AllPassed(IReadOnlyList<Bid> bids) 
            => base.AllPassed(bids);
    }
    private static IEnumerable<TestCaseData> GetNextSuitBidLevel_TestCases()
    {
        yield return new TestCaseData(
            Suit.Hearts,
            Bid.SuitBid(3, Suit.Hearts),
            4);
        
        yield return new TestCaseData(
            Suit.Spades,
            Bid.SuitBid(3, Suit.Clubs),
            3);
        
        yield return new TestCaseData(
            Suit.Clubs,
            Bid.SuitBid(1, Suit.Spades),
            2);
        
        yield return new TestCaseData(
            Suit.Clubs,
            null,
            1);
        
        yield return new TestCaseData(
            Suit.Diamonds,
            Bid.NoTrumpsBid(1),
            2);
    }
    [TestCaseSource(nameof(GetNextSuitBidLevel_TestCases))]
    public void GetNextSuitBidLevel_VariousInputs_ReturnsCorrectBidLevel(Suit suit, Bid? bid, int expected)
    {
        //Arrange
        var biddingRuleBase = new TestableBiddingRule();
        
        //Act
        var result = biddingRuleBase.GetNextSuitBidLevel(suit, bid);
        
        //Assert
        Assert.That(result, Is.EqualTo(expected));
        
    }
    
    private static IEnumerable<TestCaseData> GetNextNtBidLevel_TestCases()
    {
        yield return new TestCaseData(
            Bid.SuitBid(3, Suit.Hearts),
            3);
        
        yield return new TestCaseData(
            Bid.SuitBid(3, Suit.Clubs),
            3);
        
        yield return new TestCaseData(
            Bid.SuitBid(1, Suit.Spades),
            1);
        
        yield return new TestCaseData(
            null,
            1);
        
        yield return new TestCaseData(
            Bid.NoTrumpsBid(1),
            2);
    }
    [TestCaseSource(nameof(GetNextNtBidLevel_TestCases))]
    public void GetNextNtBidLevel_VariousInputs_ReturnsCorrectBidLevel(Bid? bid, int expected)
    {
        //Arrange
        var biddingRuleBase = new TestableBiddingRule();
        
        //Act
        var result = biddingRuleBase.GetNextNtBidLevel(bid);
        
        //Assert
        Assert.That(result, Is.EqualTo(expected));
        
    }
    
}