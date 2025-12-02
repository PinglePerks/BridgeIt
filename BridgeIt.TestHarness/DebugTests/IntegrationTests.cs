using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.DealerIntegrationTests;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.DebugTests;

[TestFixture]
public class AcolSystemTests
{
    private TestBridgeEnvironment _environment;
    
    // Path to your YAML files (adjust as needed for your machine/project structure)
    private const string AcolRulesPath = "../../../../BridgeIt.CLI/BiddingRules"; 

    [OneTimeSetUp]
    public void SetupSystem()
    {
        // 1. Build the Environment ONCE for all tests (faster)
        _environment = TestBridgeEnvironment.Create()
            .WithAllRules(AcolRulesPath);
        
            
        // Check we actually loaded rules
        // Assert.That(_environment.Engine.RuleCount, Is.GreaterThan(0)); 
    }
    
    [Test]
    public void RunScenario()
    {
        // Arrange
        var dealer = new Dealer.Deal.Dealer();
        
        //Hand Set Up
        var northHandConstraints = HandSpecifications.BalancedOpener(15,16);
        var eastHandConstraints = HandSpecifications.PreemptHand(Suit.Hearts);
        var southHandConstraints = HandSpecifications.BalancedOpener(5,12);
        
        // Create a full deal (E/W get remaining cards)
        var deal = dealer.GenerateConstrainedDeal(northHandConstraints, eastHandConstraints, southHandConstraints);

        // Act
        // Run the auction starting with North
        var auction = _environment.Table.RunAuction(deal, Seat.North);

        Console.WriteLine($"Opening Hand:     {deal[Seat.North]}\n" +
                          $"Overcaller Hand: {deal[Seat.East]}\n" +
                          $"Responder Hand:   {deal[Seat.South]}");
        
        Console.WriteLine("Final Auction Decisions:");
    }
    
    [Test]
    public void RunExactScenario()
    {
        // Arrange
        var dealer = new Dealer.Deal.Dealer();
        
        //Hand Set Up
        var northHandStr = "SA4 HAT96 DA863 C984";
        var southHandStr = "SKJ9 HQJ432 DQT2 CQ2";

        var northHand = northHandStr.ParseHand();
        var southHand = southHandStr.ParseHand();
        var allUsedCards = northHand.Cards.Concat(southHand.Cards).Select(c => c.ToSymbolString()).ToList();
        
        var fullDeck = new Deck(); // Creates a fresh 52-card deck

// Filter out the used cards to get the remaining 26
        var remainingCards = fullDeck.Cards
            .Where(c => !allUsedCards.Contains(c.ToSymbolString()) && !allUsedCards.Contains(c.ToString())) 
            // Note: Depending on your Parse implementation, you might need to match by value, not string representation.
            // A safer way if you have Card objects is:
            .Where(c => !northHand.Cards.Contains(c) && !southHand.Cards.Contains(c))
            .ToList();
        
        for (var i = remainingCards.Count - 1; i > 0; i--)
        {
            var rand = new Random();
            var j = rand.Next(i + 1);
            (remainingCards[i], remainingCards[j]) = (remainingCards[j], remainingCards[i]);
        }
        
        
        

// 5. Initialize the Dictionary
        var deal =  new Dictionary<Seat, Hand>
        {
            [Seat.North] = northHand,
            [Seat.South] = southHand,
            // Deal the remaining 26 cards to East and West
            [Seat.East]  = new (remainingCards.Take(13)),
            [Seat.West]  = new (remainingCards.Skip(13).Take(13))
        };
        
        
        // Create a full deal (E/W get remaining cards)
        // Act
        // Run the auction starting with North
        var auction = _environment.Table.RunAuction(deal, Seat.North);

        Console.WriteLine($"Opening Hand:     {deal[Seat.North]}\n" +
                          $"Overcaller Hand: {deal[Seat.East]}\n" +
                          $"Responder Hand:   {deal[Seat.South]}");
        
        Console.WriteLine("Final Auction Decisions:");
    }
}