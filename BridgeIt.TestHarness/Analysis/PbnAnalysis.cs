using BridgeIt.Analysis.Parsers;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.Analysis;

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
    public void ValidateEngineAgainstPbnFile()
    {
        var parser = new PbnParser();
        var boards = parser.ParseFile("/Users/mattyperky/Documents/pbn bridge/2146080236725618640.pbn");

        foreach (var board in boards)
        {
            Console.WriteLine($"--- Checking Board {board.BoardNumber} ---");

            foreach (var hand in board.Hands)
            {
                Console.WriteLine(hand.ToString());
            }

            // 1. Set up the table with the Real Deal
            var auction = _environment.Table.RunAuction(board.Hands, board.Dealer);

            // 2. Compare Bids
            var engineBids = auction.Select(d => d.ChosenBid.ToString()).ToList();



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
                    break;
                }
            }
        }
    }

    [Test]
    public void ValidateEngineAgainstPbnFileFirstBoard()
    {
        var parser = new PbnParser();
        var boards = parser.ParseFile("/Users/mattyperky/Documents/pbn bridge/2146080236725618640.pbn");
        var board = boards.First(b => b.BoardNumber == "5");
        Console.WriteLine($"--- Checking Board {board.BoardNumber} ---");

        foreach (var hand in board.Hands)
        {
            Console.WriteLine(hand.ToString());
        }

        // 1. Set up the table with the Real Deal
        var auction = _environment.Table.RunAuction(board.Hands, board.Dealer);

        // 2. Compare Bids
        var engineBids = auction.Select(d => d.ChosenBid.ToString()).ToList();

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
                break;
            }
        }

    }
}