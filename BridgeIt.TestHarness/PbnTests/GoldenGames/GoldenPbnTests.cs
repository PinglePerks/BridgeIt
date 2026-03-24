using BridgeIt.Analysis.Models;
using BridgeIt.Analysis.Parsers;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.PbnTests.GoldenGames;

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
        // 1. Get the path to the folder itself
        var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "GoldenGames");

        // Safety check: Make sure the folder exists so the test runner doesn't mysteriously crash
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Could not find GoldenGames folder at: {folderPath}");
        }

        // 2. Grab all .pbn files inside that folder
        var pbnFiles = Directory.GetFiles(folderPath, "*.pbn");
        var parser = new PbnParser();

        // 3. Loop through each file
        foreach (var file in pbnFiles)
        {
            var boards = parser.ParseFile(file); 
        
            // Get the file name (without the .pbn extension) to make your test names cleaner
            var fileName = Path.GetFileNameWithoutExtension(file);

            // 4. Loop through the boards in the current file
            foreach (var board in boards)
            {
                // Adding the fileName to the SetName helps you know exactly which file failed!
                yield return new TestCaseData(board).SetName($"{fileName}_Board_{board.BoardNumber}");
            }
        }
    }
    
    [Test]
    [TestCaseSource(nameof(GetGoldenHands))]
    public async Task Engine_OpeningBidPerfectly_OnGoldenGames(PbnBoard board)
    {
        var dealer = board.Dealer;
        var second = board.Dealer.GetNextSeat();
        var third = second.GetNextSeat();
        var fourth = third.GetNextSeat();
        Console.WriteLine($"{dealer}: {board.Hands[dealer]}");
        Console.WriteLine($"{second}: {board.Hands[second]}");
        Console.WriteLine($"{third}: {board.Hands[third]}");
        Console.WriteLine($"{fourth}: {board.Hands[fourth]}");
        Console.WriteLine($"****************************");
        
        Console.WriteLine($"Actual Auction: {string.Join(", ", board.ActualAuction)}");
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

            if (engineBid != "Pass" || humanBid != "Pass")
                break;
        }
    }
    
    [Test]
    [TestCaseSource(nameof(GetGoldenHands))]
    public async Task Engine_BidsPerfectly_OnGoldenGames(PbnBoard board)
    {
        var dealer = board.Dealer;
        var second = board.Dealer.GetNextSeat();
        var third = second.GetNextSeat();
        var fourth = third.GetNextSeat();
        Console.WriteLine($"{dealer}: {board.Hands[dealer]}");
        Console.WriteLine($"{second}: {board.Hands[second]}");
        Console.WriteLine($"{third}: {board.Hands[third]}");
        Console.WriteLine($"{fourth}: {board.Hands[fourth]}");
        Console.WriteLine($"****************************");
        
        Console.WriteLine($"Actual Auction: {string.Join(", ", board.ActualAuction)}");
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