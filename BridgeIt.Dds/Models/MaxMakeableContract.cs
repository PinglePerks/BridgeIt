namespace BridgeIt.Dds.Models;

/// <summary>
/// The highest makeable contract for a given side (N/S or E/W),
/// derived from the DDS trick table.
/// </summary>
public record MaxMakeableContract(
    string Strain,       // "clubs", "diamonds", "hearts", "spades", "notrump"
    int Level,           // 1–7 (tricks − 6)
    string Declarer,     // "N", "E", "S", "W"
    int Tricks           // raw trick count (7–13)
);

public static class MaxMakeableCalculator
{
    private static readonly string[] Strains = DdsTrickTable.Strains;

    /// <summary>
    /// For a given side (N/S or E/W), find the (strain, declarer) with the most tricks.
    /// Returns null if no contract above 1-level is makeable (i.e. best tricks ≤ 6).
    /// </summary>
    public static MaxMakeableContract? ForSide(DdsTrickTable trickTable, bool isNorthSouth)
    {
        var seats = isNorthSouth ? new[] { "N", "S" } : new[] { "E", "W" };

        MaxMakeableContract? best = null;

        foreach (var seat in seats)
        {
            if (!trickTable.Tricks.TryGetValue(seat, out var seatTricks))
                continue;

            foreach (var strain in Strains)
            {
                if (!seatTricks.TryGetValue(strain, out var tricks))
                    continue;

                if (tricks <= 6) continue; // no makeable contract

                var level = tricks - 6;

                if (best == null || level > best.Level ||
                    (level == best.Level && StrainRank(strain) > StrainRank(best.Strain)))
                {
                    best = new MaxMakeableContract(strain, level, seat, tricks);
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Rank strains for tie-breaking: NT > S > H > D > C (higher is better).
    /// </summary>
    private static int StrainRank(string strain) => strain switch
    {
        "notrump" => 4,
        "spades" => 3,
        "hearts" => 2,
        "diamonds" => 1,
        "clubs" => 0,
        _ => -1,
    };

    /// <summary>
    /// Map a strain key to a display string like "♠", "♥", "NT" etc.
    /// </summary>
    public static string StrainDisplay(string strain) => strain switch
    {
        "notrump" => "NT",
        "spades" => "S",
        "hearts" => "H",
        "diamonds" => "D",
        "clubs" => "C",
        _ => strain,
    };
}
