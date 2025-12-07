using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.DealerIntegrationTests;
using BridgeIt.TestHarness.DebugTests;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.RealGameTests;

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
    [TestCaseSource(typeof(RealGameIntegrationTestCases), nameof(RealGameIntegrationTestCases.NonGameTestCase))]
    public void RunScenario(string dealStr, Seat dealer, List<string> expectedBidSequence)
    {
        var deal = SimpleHandParser.ParseBoard(dealStr);

        // Act
        // Run the auction starting with North
        var auction = _environment.Table.RunAuction(deal, dealer);
        
        Console.WriteLine("Final Auction Decisions:");
        foreach (var decision in auction)
        {
            Console.WriteLine($"Bid: {decision.Decision.ChosenBid,-5} | {decision.Decision.Explanation}");
        }

        // Assert
        int sequenceIndex = 0;
        // Start at 0 (North), step 2 (Skip East), check South, step 2 (Skip West)...
        for (int i = 0; i < auction.Count && sequenceIndex < expectedBidSequence.Count; i += 1)
        {
            var actualBid = auction[i].Decision.ChosenBid.ToString();
            var expectedBid = expectedBidSequence[sequenceIndex];
            var reason = auction[i].Decision.Explanation;

            Assert.That(actualBid, Is.EqualTo(expectedBid), 
                $"Mismatch at Move {i} (Player {auction[i]}). \n" +
                $"Expected: {expectedBid}\n" +
                $"Actual:   {actualBid}\n" +
                $"Reason:   {reason}\n" +
                $"Opening Hand:     {deal[Seat.North]}\n" +
                $"Responder Hand:   {deal[Seat.South]}") ;
            sequenceIndex++;
        }
        
        // Ensure we didn't stop early
        Assert.That(sequenceIndex, Is.EqualTo(expectedBidSequence.Count), "Auction ended before expected sequence completed.");
        
        Console.WriteLine($"Opening Hand:     {deal[Seat.North]}\n" +
                          $"Responder Hand:   {deal[Seat.South]}");
    }
    
}