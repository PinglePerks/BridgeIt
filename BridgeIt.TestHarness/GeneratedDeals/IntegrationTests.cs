using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.IntegrationTests;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.DealerIntegrationTests;

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
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.TransferToSpadesScenarios))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.TransferToHeartsScenarios))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.InvitationalNtScenarios))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.WeakPassScenarios))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.MinRespondHand))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.AgreeAMajorFit))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.SlamHands))]
    [TestCaseSource(typeof(IntegrationTestCases), nameof(IntegrationTestCases.OpenerRebid))]
    
    public async Task RunScenario(Func<Hand, bool> northHandStr, Func<Hand, bool> southHandStr, List<string> expectedBidSequence)
    {
        // Arrange
        var dealer = new Dealer.Deal.Dealer();
        
        // Create a full deal (E/W get remaining cards)
        var deal = dealer.GenerateConstrainedDeal(northHandStr, southHandStr);

        Console.WriteLine("\n--- Hands Dealt ---");
        foreach(var seat in deal.Keys) Console.WriteLine($"{seat}: {deal[seat]}");
        Console.WriteLine("-------------------\n");
        // Act
        // Run the auction starting with North
        var auction = await _environment.Table.RunAuction(deal,_environment.Players, Seat.North);
        
        Console.WriteLine("Final Auction Decisions:");
        foreach (var decision in auction.Bids)
        {
            Console.WriteLine($"Bid: {decision.Bid,-5} | {decision.Bid}");
        }

        // Assert
        int sequenceIndex = 0;
        // Start at 0 (North), step 2 (Skip East), check South, step 2 (Skip West)...
        for (int i = 0; i < auction.Bids.Count && sequenceIndex < expectedBidSequence.Count; i += 2)
        {
            var actualBid = auction.Bids[i].Bid.ToString();
            var expectedBid = expectedBidSequence[sequenceIndex];
            var reason = auction.Bids[i].Bid;

            Assert.That(actualBid, Is.EqualTo(expectedBid), 
                $"Mismatch at Move {i} (Player {auction.Bids[i]}). \n" +
                $"Expected: {expectedBid}\n" +
                $"Actual:   {actualBid}\n" +
                $"Reason:   {reason}\n" +
                $"Opening Hand:     {deal[Seat.North]}\n" +
                $"Responder Hand:   {deal[Seat.South]}") ;
            sequenceIndex++;
        }
        
        // Ensure we didn't stop early
        Assert.That(sequenceIndex, Is.EqualTo(expectedBidSequence.Count), "Auction ended before expected sequence completed.");
        
    }
    
}