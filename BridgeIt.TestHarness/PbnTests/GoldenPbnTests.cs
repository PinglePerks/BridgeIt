using BridgeIt.TestHarness.Setup;
using BridgeIt.Analysis.Parsers;
using BridgeIt.Analysis.Models;
using NUnit.Framework;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.TestHarness.PbnTests;

[TestFixture]
public class GoldenPbnTests
{
    private TestBridgeEnvironment _environment;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
    }

    // This method reads your PBN file and feeds each game to the test below
    public static IEnumerable<TestCaseData> GetGoldenHands()
    {
        // Make sure "GoldenGames.pbn" is set to "Copy to Output Directory" in your .csproj!
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "GoldenGames.pbn");
        var parser = new PbnParser();
        var boards = parser.ParseFile(filePath); // Assuming this returns IEnumerable<PbnBoard>

        foreach (var board in boards)
        {
            yield return new TestCaseData(board).SetName($"GoldenGame_Board_{board.BoardNumber}");
        }
    }
    [Test]
    [TestCaseSource(nameof(GetGoldenHands))]
    public async Task Engine_OpeningBidPerfectly_OnGoldenGames(PbnBoard board)
    {
        // Act
        // Assuming your PbnBoard has a property for Dealer and the 4 Hands
        var auction = await _environment.Table.RunAuction(board.Hands, _environment.Players, board.Dealer);

        // Assert
        // Compare the engine's auction against the recorded human auction
        int movesToCompare = Math.Min(auction.Bids.Count, board.ActualAuction.Count);
        
        for (int i = 0; i < movesToCompare; i++)
        {
            var engineBid = auction.Bids[i].Bid.ToString();
            var humanBid = board.ActualAuction[i];
            
            Assert.That(engineBid, Is.EqualTo(humanBid), 
                $"Deviation on move {i + 1}. Engine bid {engineBid}, Human bid {humanBid}. " +
                $"Previous auction: {string.Join(", ", board.ActualAuction.Take(i))}");

            if (engineBid != "Pass")
                break;
        }
    }
    
    [Test]
    [TestCaseSource(nameof(GetGoldenHands))]
    public async Task Engine_BidsPerfectly_OnGoldenGames(PbnBoard board)
    {
        // Act
        // Assuming your PbnBoard has a property for Dealer and the 4 Hands
        var auction = await _environment.Table.RunAuction(board.Hands, _environment.Players, board.Dealer);

        // Assert
        // Compare the engine's auction against the recorded human auction
        int movesToCompare = Math.Min(auction.Bids.Count, board.ActualAuction.Count);
        
        for (int i = 0; i < movesToCompare; i++)
        {
            var engineBid = auction.Bids[i].Bid.ToString();
            var humanBid = board.ActualAuction[i];
            
            Assert.That(engineBid, Is.EqualTo(humanBid), 
                $"Deviation on move {i + 1}. Engine bid {engineBid}, Human bid {humanBid}. " +
                $"Previous auction: {string.Join(", ", board.ActualAuction.Take(i))}");
        }
    }
}