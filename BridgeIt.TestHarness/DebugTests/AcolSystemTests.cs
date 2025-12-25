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
    public async Task RunScenario()
    {
        // Arrange
        var dealer = new Dealer.Deal.Dealer();
        
        //Hand Set Up
        var northHandConstraints = HandSpecifications.BalancedOpener(15,17);
        var eastHandConstraints = HandSpecifications.WeakPass;
        var southHandConstraints = HandSpecifications.BalancedOpener(15,17);
        
        // Create a full deal (E/W get remaining cards)
        var deal = dealer.GenerateConstrainedDeal(northHandConstraints, eastHandConstraints, southHandConstraints);

        // Act
        // Run the auction starting with North
        var auction = await _environment.Table.RunAuction(deal,_environment.Players, Seat.North);

        Console.WriteLine($"Opening Hand:     {deal[Seat.North]}\n" +
                          $"Overcaller Hand: {deal[Seat.East]}\n" +
                          $"Responder Hand:   {deal[Seat.South]}");
        
        Console.WriteLine("Final Auction Decisions:");
    }
    
    [Test]
    public async Task RunExactScenario()
    {
        // Arrange
        var fullString = "North: AKJ7 QT86 J8 AKT\nEast: T854 7 372 J8764\nSouth: 96 AKJ93 AK5 Q92\nWest: Q32 542 QT964 53\n";
            

        var deal = SimpleHandParser.ParseBoard(fullString);
        //Hand Set Up
        
        // Create a full deal (E/W get remaining cards)
        // Act
        // Run the auction starting with North
        var auction = await _environment.Table.RunAuction(deal,_environment.Players, Seat.North);

        Console.WriteLine($"North Hand:     {deal[Seat.North]}\n" +
                          $"East Hand: {deal[Seat.East]}\n" +
                          $"South Hand:   {deal[Seat.South]}"+
                          $"West Hand:   {deal[Seat.West]}");

        
        foreach (var decision in auction.Bids)
        {
            Console.WriteLine($"Bid: {decision.Bid,-5}");
        }
    }
}