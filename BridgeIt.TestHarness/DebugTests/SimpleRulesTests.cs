using Bogus;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.Setup;
using Moq;
using NUnit.Framework;

namespace BridgeIt.TestHarness.DebugTests;

[TestFixture]
public class SimpleRulesTests
{
    private static readonly Faker Faker = new ();
    private static BiddingContext GetContext(
        Hand hand,
        AuctionHistory? auctionHistory = null,
        AuctionEvaluation? auctionEvaluation = null,
        HandEvaluation? handEvaluation = null,
        PartnershipKnowledge? partnershipKnowledge = null,
        Seat seat = Seat.North,
        Vulnerability vulnerability = Vulnerability.None
        )
        => new (
            hand,
            auctionHistory ?? new AuctionHistory(new List<AuctionBid>(), seat),
            seat, 
            vulnerability, 
            handEvaluation ?? new HandEvaluation(), 
            partnershipKnowledge ?? Mock.Of<PartnershipKnowledge>(), 
            auctionEvaluation ?? Mock.Of<AuctionEvaluation>());

    private static IEnumerable<TestCaseData> BasicAcol2LevelOpeningTestCases()
    {
        var openingRuleString = "../../../../BridgeIt.CLI/BiddingRules/Opener/Acol-2Level_Openings.yaml";
        
        yield return new TestCaseData(
            openingRuleString,
            new HandEvaluation
            {
                Hcp = Faker.Random.Int(20, 22),
                IsBalanced = true
            },
            Bid.NoTrumpsBid(2))
            .SetName("Balanced_2NT");
        
        yield return new TestCaseData(
            openingRuleString,
            new HandEvaluation
            {
                Hcp = 23,
                IsBalanced = true,
            },
            Bid.SuitBid(2, Suit.Clubs))
            .SetName("Unbalanced_5Spades_1S");
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = Faker.Random.Int(6, 10),
                    Shape =
                    {
                        {Suit.Diamonds, 6},
                    }
                },
                Bid.SuitBid(2, Suit.Diamonds))
            .SetName("6 diamonds");
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = Faker.Random.Int(6, 10),
                    Shape =
                    {
                        {Suit.Hearts, 6},
                    }
                },
                Bid.SuitBid(2, Suit.Hearts))
            .SetName("Balanced_2NT");
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = Faker.Random.Int(6, 10),
                    Shape =
                    {
                        {Suit.Spades, 6},
                    }
                },
                Bid.SuitBid(2, Suit.Spades))
            .SetName("Balanced_2NT");
    }
    
private static IEnumerable<TestCaseData> BasicAcolOpeningTestCases()
    {
        var openingRuleString = "../../../../BridgeIt.CLI/BiddingRules/Opener/Acol-Basic_Openings.yaml";
        
        //balanced 1NT
        yield return new TestCaseData(
            openingRuleString,
            new HandEvaluation
            {
                Hcp = 12,
                IsBalanced = true
            },
            Bid.NoTrumpsBid(1))
            .SetName("Balanced_1NT");
        
        yield return new TestCaseData(
            openingRuleString,
            new HandEvaluation
            {
                Hcp = 12,
                IsBalanced = false,
                Shape = new Dictionary<Suit, int>
                {
                    { Suit.Spades, 4 },
                    { Suit.Hearts, 4 },
                    { Suit.Diamonds, 4 },
                    { Suit.Clubs, 4 }
                }
            },
            Bid.SuitBid(1, Suit.Spades))
            .SetName("Unbalanced_5Spades_1S");    
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = 12,
                    IsBalanced = false,
                    Shape = new Dictionary<Suit, int>
                    {
                        { Suit.Spades, 5 },
                        { Suit.Hearts, 5 },
                        { Suit.Diamonds, 5 },
                        { Suit.Clubs, 5 }
                    }
                },
                Bid.SuitBid(1, Suit.Spades))
            .SetName("Unbalanced_5Spades+Hearts_1S"); 
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = 12,
                    IsBalanced = false,
                    Shape = new Dictionary<Suit, int>
                    {
                        { Suit.Spades, 3 },
                        { Suit.Hearts, 5 },
                        { Suit.Diamonds, 5 },
                        { Suit.Clubs, 3 }
                    }
                },
                Bid.SuitBid(1, Suit.Hearts))
            .SetName("Unbalanced_5Hearts+Diamonds_15"); 
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = 12,
                    IsBalanced = false,
                    Shape = new Dictionary<Suit, int>
                    {
                        { Suit.Spades, 4 },
                        { Suit.Hearts, 5 },
                        { Suit.Diamonds, 5 },
                        { Suit.Clubs, 5 }
                    }
                },
                Bid.SuitBid(1, Suit.Hearts))
            .SetName("Unbalanced_4Spades+5Hearts_1H"); 
        
        yield return new TestCaseData(
                openingRuleString,
                new HandEvaluation
                {
                    Hcp = 12,
                    IsBalanced = false,
                    Shape = new Dictionary<Suit, int>
                    {
                        { Suit.Spades, 5 },
                        { Suit.Hearts, 6 },
                        { Suit.Diamonds, 4 },
                        { Suit.Clubs, 6 }
                    }
                },
                Bid.SuitBid(1, Suit.Hearts))
            .SetName("Unbalanced_4Spades+5Hearts_1H"); 
    }
    
    
    
    [Test]
    [TestCaseSource(nameof(BasicAcolOpeningTestCases))]
    [TestCaseSource(nameof(BasicAcol2LevelOpeningTestCases))]
    public void SimpleRulesTest(string rulePath, HandEvaluation handEvaluation, Bid expectedBid)
    {
        var env = TestBridgeEnvironment.Create().WithSpecificRules(rulePath);
        
        var hand = new Hand(new List<Card>());
        
        var ctx = GetContext(hand: hand, handEvaluation: handEvaluation);
        
        var result = env.Engine.ChooseBid(ctx);
        
        Assert.That(result.ChosenBid.Level, Is.EqualTo(expectedBid.Level));
        
        Assert.That(result.ChosenBid.Suit, Is.EqualTo(expectedBid.Suit));

    }   
}