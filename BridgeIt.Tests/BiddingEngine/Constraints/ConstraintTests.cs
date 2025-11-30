using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using Moq;
// Used for mocking IBidConstraint in Composite/Or tests


namespace BridgeIt.Tests.BiddingEngine.Constraints;

[TestFixture]
public class AllConstraintTests
{
    // ==============================================================================
    // 1. LOGIC CONSTRAINTS (Balanced, LosingTrickCount, SuitLength)
    // ==============================================================================

    [Test]
    public void BalancedConstraint_ReturnsValueFromEvaluation()
    {
        var constraint = new BalancedConstraint();
        
        // Case 1: True
        var ctxTrue = TestHelper.CreateContext(eval: new HandEvaluation { IsBalanced = true });
        Assert.That(constraint.IsMet(ctxTrue));

        // Case 2: False
        var ctxFalse = TestHelper.CreateContext(eval: new HandEvaluation { IsBalanced = false });
        Assert.That(!constraint.IsMet(ctxFalse));
    }

    [TestCase("<=7", 7, true)]
    [TestCase("<=7", 6, true)]
    [TestCase("<=7", 8, false)]
    [TestCase("5", 5, true)] // Exact match parsing (logic in constructor falls back to exact if no <=)
    public void LosingTrickCountConstraint_EvaluatesCorrectly(string rule, int actualLtc, bool expected)
    {
        var constraint = new LosingTrickCountConstraint(rule);
        var ctx = TestHelper.CreateContext(eval: new HandEvaluation { Losers = actualLtc });
        Assert.That(expected, Is.EqualTo(constraint.IsMet(ctx)));
    }

    [TestCase("hearts", ">=5", 5, true)]
    [TestCase("hearts", ">=5", 4, false)]
    [TestCase("spades", "<=2", 2, true)]
    [TestCase("spades", "<=2", 3, false)]
    [TestCase("clubs", ">=4", 0, false)] // Missing key test (default 0)
    public void SuitLengthConstraint_EvaluatesCorrectly(string suitStr, string rule, int count, bool expected)
    {
        var constraint = new SuitLengthConstraint(suitStr, rule);
        
        var shape = new Dictionary<Suit, int>();
        // Only add the suit if count > 0 to test TryGetValue logic for missing keys
        if (count > 0) 
        {
            shape[suitStr.ToSuit()] = count;
        }

        var ctx = TestHelper.CreateContext(eval: new HandEvaluation { Shape = shape });
        
        Assert.That(expected, Is.EqualTo(constraint.IsMet(ctx)));
    }

    // ==============================================================================
    // 2. SYSTEM STATE CONSTRAINTS (CurrentState, HistoryPattern)
    // ==============================================================================

    [Test]
    public void CurrentStateConstraint_MatchesPartnershipState()
    {
        var constraint = new CurrentStateConstraint("opening_bid");
        
        var ctxMatch = TestHelper.CreateContext(aucEval: new AuctionEvaluation { PartnershipState = "opening_bid" });
        Assert.That(constraint.IsMet(ctxMatch));

        var ctxNoMatch = TestHelper.CreateContext(aucEval: new AuctionEvaluation { PartnershipState = "response" });
        Assert.That(!constraint.IsMet(ctxNoMatch));
    }
    

    // ==============================================================================
    // 3. META CONSTRAINTS (Composite, Or)
    // ==============================================================================

    [Test]
    public void CompositeConstraint_AndLogic_RequiresAllTrue()
    {
        var composite = new CompositeConstraint();
        var alwaysTrue = new Mock<IBidConstraint>();
        alwaysTrue.Setup(x => x.IsMet(It.IsAny<BiddingContext>())).Returns(true);
        
        var alwaysFalse = new Mock<IBidConstraint>();
        alwaysFalse.Setup(x => x.IsMet(It.IsAny<BiddingContext>())).Returns(false);

        var ctx = TestHelper.CreateContext();

        // 1. Empty -> True (All 0 items are true)
        Assert.That(composite.IsMet(ctx));

        // 2. All True -> True
        composite.Add(alwaysTrue.Object);
        Assert.That(composite.IsMet(ctx));

        // 3. One False -> False
        composite.Add(alwaysFalse.Object);
        Assert.That(!composite.IsMet(ctx));
    }

    [Test]
    public void OrConstraint_OrLogic_RequiresAnyTrue()
    {
        var orConstraint = new OrConstraint();
        var alwaysTrue = new Mock<IBidConstraint>();
        alwaysTrue.Setup(x => x.IsMet(It.IsAny<BiddingContext>())).Returns(true);
        
        var alwaysFalse = new Mock<IBidConstraint>();
        alwaysFalse.Setup(x => x.IsMet(It.IsAny<BiddingContext>())).Returns(false);

        var ctx = TestHelper.CreateContext();

        // 1. Empty -> False (Any of 0 items is false)
        Assert.That(!orConstraint.IsMet(ctx));

        // 2. All False -> False
        orConstraint.Add(alwaysFalse.Object);
        Assert.That(!orConstraint.IsMet(ctx));

        // 3. One True -> True
        orConstraint.Add(alwaysTrue.Object);
        Assert.That(orConstraint.IsMet(ctx));
    }

    // ==============================================================================
    // 4. PARTNER KNOWLEDGE CONSTRAINT
    // ==============================================================================

    [Test]
    public void PartnerKnowledge_CombinedHcp_EvaluatesCorrectly()
    {
        // Scenario: Partner showed 12-14. We have 13. Total 25.
        // Requirement: >= 25.
        
        var requirements = new Dictionary<string, string> { { "combined_hcp", ">=25" } };
        var constraint = new PartnerKnowledgeConstraint(requirements);

        var knowledge = new PartnershipKnowledge { PartnerHcpMin = 12 };
        var myEval = new HandEvaluation { Hcp = 13 }; // 12 + 13 = 25

        var ctx = TestHelper.CreateContext(eval: myEval, knowledge: knowledge);

        // 25 >= 25 -> True
        Assert.That(constraint.IsMet(ctx));

        // If I have 12 HCP -> Total 24 -> False
        var ctxLow = TestHelper.CreateContext(eval: new HandEvaluation { Hcp = 12 }, knowledge: knowledge);
        Assert.That(!constraint.IsMet(ctxLow));
    }

    [Test]
    public void PartnerKnowledge_FitInSuit_EvaluatesCorrectly()
    {
        var requirements = new Dictionary<string, string> { { "fit_in_suit", "hearts" } };
        var constraint = new PartnerKnowledgeConstraint(requirements);

        var knowledge = new PartnershipKnowledge();
        knowledge.PartnerMinShape[Suit.Hearts] = 4; // Partner has 4 hearts

        // Case 1: I have 4 Hearts (4+4=8) -> Fit!
        var evalFit = new HandEvaluation { Shape = new Dictionary<Suit, int> { { Suit.Hearts, 4 } } };
        Assert.That(constraint.IsMet(TestHelper.CreateContext(eval: evalFit, knowledge: knowledge)));

        // Case 2: I have 3 Hearts (4+3=7) -> No Fit
        var evalNoFit = new HandEvaluation { Shape = new Dictionary<Suit, int> { { Suit.Hearts, 3 } } };
        Assert.That(!constraint.IsMet(TestHelper.CreateContext(eval: evalNoFit, knowledge: knowledge)));
    }

    [Test]
    public void PartnerKnowledge_UnknownKey_IsIgnoredAndReturnsTrue()
    {
        // Default behavior in your switch statement is to log and continue, eventually returning true
        var requirements = new Dictionary<string, string> { { "unknown_key", "some_value" } };
        var constraint = new PartnerKnowledgeConstraint(requirements);

        Assert.That(constraint.IsMet(TestHelper.CreateContext()));
    }
}

// ==============================================================================
// TEST HELPER CLASS
// ==============================================================================
public static class TestHelper
{
    public static BiddingContext CreateContext(
        HandEvaluation? eval = null,
        AuctionEvaluation? aucEval = null,
        PartnershipKnowledge? knowledge = null,
        string[]? historyStrs = null)
    {
        // 1. Defaults
        var handEval = eval ?? new HandEvaluation 
        { 
            Hcp = 0, IsBalanced = false, Losers = 0, Shape = new Dictionary<Suit, int>() 
        };
        
        var auctionEval = aucEval ?? new AuctionEvaluation 
        { 
            PartnershipState = "default" 
        };
        
        var partnerKnowledge = knowledge ?? new PartnershipKnowledge();

        // 2. Build History
        var bidList = new List<BiddingDecision>();
        if (historyStrs != null)
        {
            foreach (var s in historyStrs)
            {
                var bid = s == "Pass" ? Bid.Pass() : s.ToBid();
                bidList.Add(new BiddingDecision(bid, "", ""));
            }
        }
        var auctionHistory = new AuctionHistory(bidList, Seat.North);

        // 3. Dummy Objects
        var dummyHand = new Hand(new List<Card>());

        return new BiddingContext(
            dummyHand,
            auctionHistory,
            Seat.North,
            Vulnerability.None,
            handEval,
            partnerKnowledge,
            auctionEval
        );
    }
}