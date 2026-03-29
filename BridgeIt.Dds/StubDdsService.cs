using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dds.Models;

namespace BridgeIt.Dds;

/// <summary>
/// Stub DDS service that returns plausible trick counts based on HCP distribution.
/// Used during development until the real DDS native library is compiled and linked.
/// </summary>
public class StubDdsService : IDdsService
{
    public DdsAnalysis Analyse(Dictionary<Seat, Hand> deal, Seat dealer)
    {
        var trickTable = ComputeStubTrickTable(deal);
        var par = new Dictionary<string, ParResult>();

        foreach (var vulKey in DdsAnalysis.VulnerabilityKeys)
        {
            par[vulKey] = ComputeStubPar(trickTable, vulKey);
        }

        return new DdsAnalysis
        {
            TrickTable = trickTable,
            Par = par
        };
    }

    private static DdsTrickTable ComputeStubTrickTable(Dictionary<Seat, Hand> deal)
    {
        var hcp = new Dictionary<Seat, int>();
        foreach (var (seat, hand) in deal)
        {
            hcp[seat] = hand.Cards.Sum(c => c.Rank switch
            {
                Rank.Ace => 4,
                Rank.King => 3,
                Rank.Queen => 2,
                Rank.Jack => 1,
                _ => 0
            });
        }

        var tricks = new Dictionary<string, Dictionary<string, int>>();
        var seats = new[] { Seat.North, Seat.East, Seat.South, Seat.West };
        var rng = new Random(hcp.Values.Sum() * 17 + deal[Seat.North].Cards[0].GetHashCode());

        foreach (var seat in seats)
        {
            var seatKey = DdsTrickTable.SeatToKey(seat);
            var partner = seat.GetPartner();
            var combinedHcp = hcp[seat] + hcp[partner];
            var seatTricks = new Dictionary<string, int>();

            foreach (var strain in DdsTrickTable.Strains)
            {
                // Base tricks from combined HCP (roughly: 6 + (combinedHcp - 20) * 0.3)
                var baseTricks = 6.0 + (combinedHcp - 20) * 0.3;

                // Suit fit bonus: longer combined suit length = more tricks
                if (strain != "notrump")
                {
                    var suit = StrainToSuit(strain);
                    var fitLength = deal[seat].CountSuit(suit) + deal[partner].CountSuit(suit);
                    baseTricks += (fitLength - 7) * 0.5; // bonus for 8+ fit
                }

                // Add small random jitter for realism
                baseTricks += (rng.NextDouble() - 0.5) * 1.5;

                seatTricks[strain] = Math.Clamp((int)Math.Round(baseTricks), 0, 13);
            }

            tricks[seatKey] = seatTricks;
        }

        // Ensure N/S and E/W partnership consistency (partners make same tricks)
        // and opponent tricks are 13 - declarer tricks
        foreach (var strain in DdsTrickTable.Strains)
        {
            var nTricks = tricks["N"][strain];
            var sTricks = tricks["S"][strain];
            var avgNs = (nTricks + sTricks + 1) / 2;
            tricks["N"][strain] = avgNs;
            tricks["S"][strain] = avgNs;
            tricks["E"][strain] = 13 - avgNs;
            tricks["W"][strain] = 13 - avgNs;
        }

        return new DdsTrickTable { Tricks = tricks };
    }

    private static ParResult ComputeStubPar(DdsTrickTable trickTable, string vulKey)
    {
        var isNsVul = vulKey is "nsVul" or "bothVul";
        var isEwVul = vulKey is "ewVul" or "bothVul";

        // Find the best contract for each side
        var bestNs = FindBestContract(trickTable, "N", isNsVul);
        var bestEw = FindBestContract(trickTable, "E", isEwVul);

        if (bestNs.score >= bestEw.score)
        {
            return new ParResult
            {
                Type = ParType.Make,
                Contract = bestNs.contract,
                Doubled = false,
                Declarer = bestNs.declarer,
                Tricks = bestNs.tricks,
                Score = bestNs.score,
                ScoringSide = "NS"
            };
        }
        else
        {
            return new ParResult
            {
                Type = ParType.Make,
                Contract = bestEw.contract,
                Doubled = false,
                Declarer = bestEw.declarer,
                Tricks = bestEw.tricks,
                Score = bestEw.score,
                ScoringSide = "EW"
            };
        }
    }

    private static (string contract, string declarer, int tricks, int score) FindBestContract(
        DdsTrickTable trickTable, string seatKey, bool vulnerable)
    {
        var bestScore = 0;
        var bestContract = "Pass";
        var bestDeclarer = seatKey;
        var bestTricks = 0;

        foreach (var strain in DdsTrickTable.Strains)
        {
            var tricks = trickTable.Tricks[seatKey][strain];
            if (tricks < 7) continue; // Need at least 7 tricks for a 1-level contract

            var level = tricks - 6;
            var score = CalculateScore(strain, level, tricks, vulnerable);

            if (score > bestScore)
            {
                bestScore = score;
                bestContract = $"{level}{StrainSymbol(strain)}";
                bestDeclarer = seatKey;
                bestTricks = tricks;
            }
        }

        return (bestContract, bestDeclarer, bestTricks, bestScore);
    }

    private static int CalculateScore(string strain, int level, int tricks, bool vulnerable)
    {
        var isMinor = strain is "clubs" or "diamonds";
        var isMajor = strain is "hearts" or "spades";
        var isNt = strain == "notrump";

        // Trick value
        var trickValue = isMinor ? 20 : isMajor ? 30 : 0;
        var contractPoints = 0;

        if (isNt)
        {
            contractPoints = 40 + (level - 1) * 30;
        }
        else
        {
            contractPoints = level * trickValue;
        }

        var isGame = contractPoints >= 100;
        var isSmallSlam = level == 6;
        var isGrandSlam = level == 7;

        var score = contractPoints;

        // Game/part-score bonus
        if (isGame)
            score += vulnerable ? 500 : 300;
        else
            score += 50;

        // Slam bonuses
        if (isSmallSlam) score += vulnerable ? 750 : 500;
        if (isGrandSlam) score += vulnerable ? 1500 : 1000;

        // Overtricks
        var overTricks = tricks - (level + 6);
        score += overTricks * (isNt ? 30 : trickValue);

        return score;
    }

    private static string StrainSymbol(string strain) => strain switch
    {
        "clubs" => "C",
        "diamonds" => "D",
        "hearts" => "H",
        "spades" => "S",
        "notrump" => "NT",
        _ => "?"
    };

    private static Suit StrainToSuit(string strain) => strain switch
    {
        "clubs" => Suit.Clubs,
        "diamonds" => Suit.Diamonds,
        "hearts" => Suit.Hearts,
        "spades" => Suit.Spades,
        _ => throw new ArgumentException($"Not a suit strain: {strain}")
    };
}
