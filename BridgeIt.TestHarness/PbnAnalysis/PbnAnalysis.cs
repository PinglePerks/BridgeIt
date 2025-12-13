using BridgeIt.Analysis.Parsers;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.PbnAnalysis;

[TestFixture]
public class PbnAnalysis
{
    private TestBridgeEnvironment _environment;
    private const string AcolRulesPath = "../../../../BridgeIt.CLI/BiddingRules";
    
    [OneTimeSetUp]
    public void SetupSystem()
    {
        // 1. Build the Environment ONCE for all tests (faster)
        _environment = TestBridgeEnvironment.Create()
            .WithAllRules(AcolRulesPath);
    }

    [Test]
    public async Task ValidateEngineAgainstPbnFile()
    {
        var parser = new PbnParser();
        var boards = parser.ParseFile("/Users/mattyperky/Documents/pbn bridge/214550427545623544460.pbn");

        foreach (var board in boards)
        {
            Console.WriteLine($"--- Checking Board {board.BoardNumber} ---");

            foreach (var hand in board.Hands)
            {
                Console.WriteLine(hand.ToString());
            }

            // 1. Set up the table with the Real Deal
            var auction = await _environment.Table.RunAuction(board.Hands,_environment.Players, board.Dealer);

            // 2. Compare Bids
            var engineBids = auction.Bids.Select(d => d.Bid.ToString()).ToList();
            
            // Basic Comparison Loop
            int movesToCompare = Math.Min(engineBids.Count, board.ActualAuction.Count);
            for (int i = 0; i < movesToCompare; i++)
            {
                // Normalize "X" vs "Double" if needed
                string actual = board.ActualAuction[i];
                string engine = engineBids[i];
                Console.WriteLine($"Checking Move {i + 1}");
                Console.WriteLine($"{actual} vs {engine}");

                if (actual != engine)
                {
                    Console.WriteLine($"Deviation at Move {i + 1}: Human {actual} vs Engine {engine}");
                    // In early dev, you break here. 
                    // Later, you might just count stats (e.g., "Matched 85% of openers")
                }
            }
        }
    }

    [Test]
    public async Task ValidateEngineAgainstPbnFileFirstBoard()
    {
        var parser = new PbnParser();
        var boards = parser.ParseFile("/Users/mattyperky/Downloads/214550427545623544460.pbn");
        var board = boards.First(b => b.BoardNumber == "83");
        Console.WriteLine($"--- Checking Board {board.BoardNumber} ---");

        foreach (var hand in board.Hands)
        {
            Console.WriteLine(hand.ToString());
        }

        // 1. Set up the table with the Real Deal
        var auction = await _environment.Table.RunAuction(board.Hands, _environment.Players, board.Dealer);

        // 2. Compare Bids
        var engineBids =  auction.Bids.Select(d => d.Bid.ToString()).ToList();

        // Basic Comparison Loop
        int movesToCompare = Math.Min(engineBids.Count, board.ActualAuction.Count);
        for (int i = 0; i < movesToCompare; i++)
        {
            // Normalize "X" vs "Double" if needed
            string actual = board.ActualAuction[i];
            string engine = engineBids[i];
            Console.WriteLine($"Checking Move {i + 1}");
            Console.WriteLine($"{actual} vs {engine}");

            if (actual != engine)
            {
                Console.WriteLine($"Deviation at Move {i + 1}: Human {actual} vs Engine {engine}");
                // In early dev, you break here. 
                // Later, you might just count stats (e.g., "Matched 85% of openers")
            }
        }

    }
}