using BridgeIt.Analysis.Models;
using BridgeIt.Analysis.Parsers;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.PbnTests.AuctionAccuracy;

[TestFixture]
public class AuctionAccuracyTests
{
    private TestBridgeEnvironment _environment;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
    }

    public static IEnumerable<TestCaseData> GetTestHands()
    {
        var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Games");

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Could not find GoldenGames folder at: {folderPath}");

        var pbnFiles = Directory.GetFiles(folderPath, "*.pbn");
        var parser = new PbnParser();

        foreach (var file in pbnFiles)
        {
            var boards = parser.ParseFile(file);
            var fileName = Path.GetFileNameWithoutExtension(file);

            foreach (var board in boards)
            {
                yield return new TestCaseData(board).SetName($"{fileName}_Board_{board.BoardNumber}");
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(GetTestHands))]
    public async Task AuctionAccuracy_GoldenGames(PbnBoard board)
    {
        await RunAccuracyTest(board);
    }

    private async Task RunAccuracyTest(PbnBoard board)
    {
        var dealer = board.Dealer;
        var second = dealer.GetNextSeat();
        var third = second.GetNextSeat();
        var fourth = third.GetNextSeat();

        Console.WriteLine($"{dealer}: {board.Hands[dealer]}");
        Console.WriteLine($"{second}: {board.Hands[second]}");
        Console.WriteLine($"{third}: {board.Hands[third]}");
        Console.WriteLine($"{fourth}: {board.Hands[fourth]}");
        Console.WriteLine("****************************");
        Console.WriteLine($"Actual Auction: {string.Join(", ", board.ActualAuction)}");

        var auction = await _environment.Table.RunAuction(board.Hands, _environment.Players, board.Dealer);

        var engineBids = auction.Bids.Select(b => b.Bid.ToString()).ToList();
        var humanBids = board.ActualAuction;
        int movesToCompare = Math.Min(engineBids.Count, humanBids.Count);

        int matches = 0;
        for (int i = 0; i < movesToCompare; i++)
        {
            var engineBid = engineBids[i];
            var humanBid = humanBids[i];
            bool isMatch = engineBid == humanBid;

            if (isMatch)
                matches++;

            Console.WriteLine($"  Move {i + 1}: Engine={engineBid,-6} Human={humanBid,-6} {(isMatch ? "OK" : "MISMATCH")}");
        }

        // Account for length differences — extra bids in either auction count as mismatches
        int totalBids = Math.Max(engineBids.Count, humanBids.Count);

        if (engineBids.Count > movesToCompare)
        {
            for (int i = movesToCompare; i < engineBids.Count; i++)
                Console.WriteLine($"  Move {i + 1}: Engine={engineBids[i],-6} Human={"---",-6} MISMATCH (engine extra)");
        }

        if (humanBids.Count > movesToCompare)
        {
            for (int i = movesToCompare; i < humanBids.Count; i++)
                Console.WriteLine($"  Move {i + 1}: Engine={"---",-6} Human={humanBids[i],-6} MISMATCH (human extra)");
        }

        double percentage = totalBids > 0 ? (double)matches / totalBids * 100 : 0;

        Console.WriteLine("****************************");
        Console.WriteLine($"Engine Auction: {string.Join(", ", engineBids)}");
        Console.WriteLine($"Match: {matches}/{totalBids} bids ({percentage:F1}%)");

        // Always pass — this is a reporting test, not an assertion test
        Assert.Pass($"{matches}/{totalBids} bids matched ({percentage:F1}%)");
    }
}
