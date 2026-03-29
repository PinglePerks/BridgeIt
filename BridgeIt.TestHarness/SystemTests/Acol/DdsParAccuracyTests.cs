using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dds;
using BridgeIt.Dds.Models;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol;

[TestFixture]
[Explicit("Benchmark test — requires native DDS library and takes several minutes")]
public class DdsParAccuracyTests
{
    private const int DealCount = 200;

    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;
    private DdsService _dds;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer();

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _dds = new DdsService(loggerFactory.CreateLogger<DdsService>());
    }

    [Test]
    public async Task BiddingEngine_VsPar_RandomDeals()
    {
        var passedOut = 0;
        var exactParMatch = 0;
        var makeable = 0;
        var totalScored = 0; // deals where both engine and par have a contract
        var totalEngineDeclScore = 0;
        var totalParScore = 0;
        var mismatches = new List<string>();

        for (var i = 0; i < DealCount; i++)
        {
            var deal = _dealer.GenerateRandomDeal();
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var engineContract = GetFinalContract(auction);
            var engineDeclarer = GetDeclarer(auction);

            DdsAnalysis ddsAnalysis;
            try
            {
                ddsAnalysis = _dds.Analyse(deal, Seat.North);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Deal {i + 1}: DDS error — {ex.Message}");
                continue;
            }

            var par = ddsAnalysis.Par["none"];

            // Both passed out
            if (engineContract == null && par.Contract == "Pass")
            {
                passedOut++;
                continue;
            }

            // Engine passed out but par has a contract (or vice versa)
            if (engineContract == null)
            {
                totalScored++;
                var parAbs = Math.Abs(par.Score);
                totalParScore += parAbs;
                mismatches.Add($"Deal {i + 1}: Engine=PassOut, Par={par.Contract} by {par.Declarer} (score {par.Score})");
                continue;
            }

            if (par.Contract == "Pass")
            {
                // Engine bid something but par says pass out — check if it's makeable
                totalScored++;
                var tricks = GetTricksForContract(ddsAnalysis.TrickTable, engineDeclarer!.Value, engineContract);
                var contractMakes = tricks >= engineContract.Level + 6;
                if (contractMakes) makeable++;
                var score = ComputeSimpleScore(engineContract, tricks);
                totalEngineDeclScore += score;
                mismatches.Add($"Deal {i + 1}: Engine={engineContract} by {SeatToKey(engineDeclarer!.Value)} ({(contractMakes ? "makes" : "fails")}), Par=PassOut");
                continue;
            }

            totalScored++;

            // Check makeability
            var engineTricks = GetTricksForContract(ddsAnalysis.TrickTable, engineDeclarer!.Value, engineContract);
            var engineMakes = engineTricks >= engineContract.Level + 6;
            if (engineMakes) makeable++;

            // Check exact par match (same level + strain, same declaring side)
            var engineContractStr = engineContract.ToString();
            var engineSide = GetSide(engineDeclarer!.Value);
            var parSide = GetSide(par.Declarer);
            if (engineContractStr == par.Contract && engineSide == parSide)
                exactParMatch++;

            // Score comparison
            var engineScore = ComputeSimpleScore(engineContract, engineTricks);
            totalEngineDeclScore += engineScore;
            totalParScore += Math.Abs(par.Score);

            if (engineContractStr != par.Contract || engineSide != parSide)
            {
                mismatches.Add(
                    $"Deal {i + 1}: Engine={engineContractStr} by {SeatToKey(engineDeclarer!.Value)} " +
                    $"(tricks={engineTricks}, {(engineMakes ? "makes" : "fails")}, score={engineScore}), " +
                    $"Par={par.Contract} by {par.Declarer} (score={par.Score})");
            }
        }

        // Print summary
        var contested = DealCount - passedOut;
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine($"=== DDS Par Accuracy ({DealCount} deals) ===");
        TestContext.Out.WriteLine($"Passed out:      {passedOut}");
        TestContext.Out.WriteLine($"Contested deals: {contested}");
        if (contested > 0)
        {
            TestContext.Out.WriteLine($"Exact par match: {exactParMatch}/{totalScored} ({100.0 * exactParMatch / totalScored:F1}%)");
            TestContext.Out.WriteLine($"Makeable:        {makeable}/{totalScored} ({100.0 * makeable / totalScored:F1}%)");
            if (totalScored > 0)
                TestContext.Out.WriteLine($"Avg engine score: {totalEngineDeclScore / totalScored}, Avg par score: {totalParScore / totalScored}");
        }

        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine($"=== Mismatches ({mismatches.Count}) ===");
        foreach (var m in mismatches)
            TestContext.Out.WriteLine(m);

        // Don't assert — this is a benchmark, not a pass/fail test
        Assert.Pass($"Benchmark complete: {exactParMatch}/{totalScored} exact par matches ({(totalScored > 0 ? 100.0 * exactParMatch / totalScored : 0):F1}%)");
    }
    
    [Test]
    public async Task BiddingEngine_VsPar_1NTOpening()
    {
        var passedOut = 0;
        var exactParMatch = 0;
        var makeable = 0;
        var totalScored = 0; // deals where both engine and par have a contract
        var totalEngineDeclScore = 0;
        var totalParScore = 0;
        var mismatches = new List<string>();

        for (var i = 0; i < DealCount; i++)
        {
            var deal = _dealer.GenerateConstrainedDeal(HandSpecification.Acol1NtOpening, null);
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var engineContract = GetFinalContract(auction);
            var engineDeclarer = GetDeclarer(auction);

            DdsAnalysis ddsAnalysis;
            try
            {
                ddsAnalysis = _dds.Analyse(deal, Seat.North);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Deal {i + 1}: DDS error — {ex.Message}");
                continue;
            }

            var par = ddsAnalysis.Par["none"];

            // Both passed out
            if (engineContract == null && par.Contract == "Pass")
            {
                passedOut++;
                continue;
            }

            // Engine passed out but par has a contract (or vice versa)
            if (engineContract == null)
            {
                totalScored++;
                var parAbs = Math.Abs(par.Score);
                totalParScore += parAbs;
                mismatches.Add($"Deal {i + 1}: Engine=PassOut, Par={par.Contract} by {par.Declarer} (score {par.Score})");
                continue;
            }

            if (par.Contract == "Pass")
            {
                // Engine bid something but par says pass out — check if it's makeable
                totalScored++;
                var tricks = GetTricksForContract(ddsAnalysis.TrickTable, engineDeclarer!.Value, engineContract);
                var contractMakes = tricks >= engineContract.Level + 6;
                if (contractMakes) makeable++;
                var score = ComputeSimpleScore(engineContract, tricks);
                totalEngineDeclScore += score;
                mismatches.Add($"Deal {i + 1}: Engine={engineContract} by {SeatToKey(engineDeclarer!.Value)} ({(contractMakes ? "makes" : "fails")}), Par=PassOut");
                continue;
            }

            totalScored++;

            // Check makeability
            var engineTricks = GetTricksForContract(ddsAnalysis.TrickTable, engineDeclarer!.Value, engineContract);
            var engineMakes = engineTricks >= engineContract.Level + 6;
            if (engineMakes) makeable++;

            // Check exact par match (same level + strain, same declaring side)
            var engineContractStr = engineContract.ToString();
            var engineSide = GetSide(engineDeclarer!.Value);
            var parSide = GetSide(par.Declarer);
            if (engineContractStr == par.Contract && engineSide == parSide)
                exactParMatch++;

            // Score comparison
            var engineScore = ComputeSimpleScore(engineContract, engineTricks);
            totalEngineDeclScore += engineScore;
            totalParScore += Math.Abs(par.Score);

            if (engineContractStr != par.Contract || engineSide != parSide)
            {
                mismatches.Add(
                    $"Deal {i + 1}: Engine={engineContractStr} by {SeatToKey(engineDeclarer!.Value)} " +
                    $"(tricks={engineTricks}, {(engineMakes ? "makes" : "fails")}, score={engineScore}), " +
                    $"Par={par.Contract} by {par.Declarer} (score={par.Score})");
            }
        }

        // Print summary
        var contested = DealCount - passedOut;
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine($"=== DDS Par Accuracy ({DealCount} deals) ===");
        TestContext.Out.WriteLine($"Passed out:      {passedOut}");
        TestContext.Out.WriteLine($"Contested deals: {contested}");
        if (contested > 0)
        {
            TestContext.Out.WriteLine($"Exact par match: {exactParMatch}/{totalScored} ({100.0 * exactParMatch / totalScored:F1}%)");
            TestContext.Out.WriteLine($"Makeable:        {makeable}/{totalScored} ({100.0 * makeable / totalScored:F1}%)");
            if (totalScored > 0)
                TestContext.Out.WriteLine($"Avg engine score: {totalEngineDeclScore / totalScored}, Avg par score: {totalParScore / totalScored}");
        }

        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine($"=== Mismatches ({mismatches.Count}) ===");
        foreach (var m in mismatches)
            TestContext.Out.WriteLine(m);

        // Don't assert — this is a benchmark, not a pass/fail test
        Assert.Pass($"Benchmark complete: {exactParMatch}/{totalScored} exact par matches ({(totalScored > 0 ? 100.0 * exactParMatch / totalScored : 0):F1}%)");
    }

    /// <summary>
    /// Extract the final contract bid from the auction (last non-Pass, non-Double, non-Redouble bid).
    /// Returns null if the auction was passed out.
    /// </summary>
    private static Bid? GetFinalContract(AuctionHistory auction)
        => auction.Bids
            .LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps)?
            .Bid;

    /// <summary>
    /// Determine the declarer: the first player in the declaring partnership
    /// who bid the final contract's strain.
    /// </summary>
    private static Seat? GetDeclarer(AuctionHistory auction)
    {
        // Find the last real bid and who made it
        var lastContractBid = auction.Bids
            .LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps);

        if (lastContractBid == null) return null;

        var declaringSeat = lastContractBid.Seat;
        var partner = declaringSeat.GetPartner();
        var strain = lastContractBid.Bid.Type == BidType.NoTrumps ? (Suit?)null : lastContractBid.Bid.Suit;
        var isNt = lastContractBid.Bid.Type == BidType.NoTrumps;

        // Walk the auction to find the first player in this partnership who bid this strain
        foreach (var bid in auction.Bids)
        {
            if (bid.Seat != declaringSeat && bid.Seat != partner) continue;

            if (isNt && bid.Bid.Type == BidType.NoTrumps)
                return bid.Seat;

            if (!isNt && bid.Bid.Type == BidType.Suit && bid.Bid.Suit == strain)
                return bid.Seat;
        }

        // Fallback (shouldn't happen)
        return declaringSeat;
    }

    private static int GetTricksForContract(DdsTrickTable trickTable, Seat declarer, Bid contract)
    {
        var strainKey = contract.Type == BidType.NoTrumps
            ? "notrump"
            : contract.Suit!.Value switch
            {
                Suit.Clubs => "clubs",
                Suit.Diamonds => "diamonds",
                Suit.Hearts => "hearts",
                Suit.Spades => "spades",
                _ => throw new ArgumentOutOfRangeException()
            };

        return trickTable.GetTricks(declarer, strainKey);
    }

    /// <summary>
    /// Simple non-vulnerable, undoubled score calculation.
    /// </summary>
    private static int ComputeSimpleScore(Bid contract, int tricks)
    {
        var level = contract.Level;
        var required = level + 6;
        var overUnder = tricks - required;

        if (overUnder < 0)
            return -(Math.Abs(overUnder) * 50); // non-vul undoubled undertricks

        var isNt = contract.Type == BidType.NoTrumps;
        var isMinor = contract.Type == BidType.Suit &&
                      contract.Suit is Suit.Clubs or Suit.Diamonds;
        var trickVal = isMinor ? 20 : 30;

        var trickScore = isNt ? 40 + (level - 1) * 30 : level * trickVal;
        var isGame = trickScore >= 100;
        var isSmallSlam = level == 6;
        var isGrandSlam = level == 7;

        var score = trickScore;
        score += isGame ? 300 : 50; // non-vul game/partscore bonus

        if (isGrandSlam) score += 1000;
        else if (isSmallSlam) score += 500;

        // Overtricks
        score += overUnder * (isNt ? 30 : trickVal);

        return score;
    }

    private static string SeatToKey(Seat seat) => seat switch
    {
        Seat.North => "N",
        Seat.East => "E",
        Seat.South => "S",
        Seat.West => "W",
        _ => "?"
    };

    private static string GetSide(Seat seat) =>
        seat is Seat.North or Seat.South ? "NS" : "EW";

    private static string GetSide(string declarerKey) =>
        declarerKey is "N" or "S" ? "NS" : "EW";
}
