using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.BiddingEngine.Core;

[TestFixture]
public class BiddingRuleBaseTests
{
    // Expose protected methods for testing
    private class TestableBiddingRule : BiddingRuleBase
    {
        public override string Name => "Testable Rule";
        public override int Priority => 0;
        public override bool CouldMakeBid(DecisionContext ctx) => false;
        public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx) => null;
        public override Bid? Apply(DecisionContext ctx) => null;
        public override bool CouldExplainBid(Bid bid, DecisionContext ctx) => false;

        public new int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
            => base.GetNextSuitBidLevel(suit, currentContract);

        public new int GetNextNtBidLevel(Bid? currentContract)
            => base.GetNextNtBidLevel(currentContract);
    }

    // =============================================
    // GetNextSuitBidLevel
    // =============================================

    private static IEnumerable<TestCaseData> GetNextSuitBidLevel_TestCases()
    {
        // Same suit at same level → must go up
        yield return new TestCaseData(Suit.Hearts, Bid.SuitBid(3, Suit.Hearts), 4)
            .SetName("SameSuit_MustGoUp");

        // Higher suit at same level → can stay at same level
        yield return new TestCaseData(Suit.Spades, Bid.SuitBid(3, Suit.Clubs), 3)
            .SetName("HigherSuit_SameLevel");

        // Lower suit than current → must go up
        yield return new TestCaseData(Suit.Clubs, Bid.SuitBid(1, Suit.Spades), 2)
            .SetName("LowerSuit_MustGoUp");

        // No current contract → level 1
        yield return new TestCaseData(Suit.Clubs, null, 1)
            .SetName("NoContract_Level1");

        // After NT → always goes up
        yield return new TestCaseData(Suit.Diamonds, Bid.NoTrumpsBid(1), 2)
            .SetName("AfterNT_MustGoUp");
    }

    [TestCaseSource(nameof(GetNextSuitBidLevel_TestCases))]
    public void GetNextSuitBidLevel_VariousInputs_ReturnsCorrectLevel(Suit suit, Bid? bid, int expected)
    {
        var rule = new TestableBiddingRule();
        var result = rule.GetNextSuitBidLevel(suit, bid);
        Assert.That(result, Is.EqualTo(expected));
    }

    // =============================================
    // GetNextNtBidLevel
    // =============================================

    private static IEnumerable<TestCaseData> GetNextNtBidLevel_TestCases()
    {
        // After a suit bid at same level → NT at same level (NT outranks suits)
        yield return new TestCaseData(Bid.SuitBid(3, Suit.Hearts), 3)
            .SetName("AfterSuit_SameLevel");

        yield return new TestCaseData(Bid.SuitBid(3, Suit.Clubs), 3)
            .SetName("AfterLowSuit_SameLevel");

        // After a suit bid at level 1 → NT at level 1
        yield return new TestCaseData(Bid.SuitBid(1, Suit.Spades), 1)
            .SetName("After1Spade_Level1");

        // No current contract → level 1
        yield return new TestCaseData(null, 1)
            .SetName("NoContract_Level1");

        // After NT → must go up
        yield return new TestCaseData(Bid.NoTrumpsBid(1), 2)
            .SetName("AfterNT_MustGoUp");
    }

    [TestCaseSource(nameof(GetNextNtBidLevel_TestCases))]
    public void GetNextNtBidLevel_VariousInputs_ReturnsCorrectLevel(Bid? bid, int expected)
    {
        var rule = new TestableBiddingRule();
        var result = rule.GetNextNtBidLevel(bid);
        Assert.That(result, Is.EqualTo(expected));
    }
}
