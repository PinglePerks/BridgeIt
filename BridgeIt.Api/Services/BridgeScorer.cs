namespace BridgeIt.Api.Services;

/// <summary>
/// Computes duplicate bridge scores from contract details.
/// All scores are from declarer's perspective (positive = declarer made, negative = set).
/// </summary>
public static class BridgeScorer
{
    /// <summary>
    /// Parse a PBN contract string like "4H", "3NTX", "2DXX", "Pass" and compute the score.
    /// </summary>
    public static int? ComputeScore(string? contract, int? tricksTaken, bool declarerVulnerable)
    {
        if (string.IsNullOrEmpty(contract) || contract == "Pass" || tricksTaken == null)
            return null;

        var parsed = ParseContract(contract);
        if (parsed == null) return null;

        var (level, strain, doubled, redoubled) = parsed.Value;
        return ComputeScore(level, strain, doubled, redoubled, tricksTaken.Value, declarerVulnerable);
    }

    public static int ComputeScore(int level, Strain strain, bool doubled, bool redoubled,
        int tricksTaken, bool vulnerable)
    {
        var requiredTricks = level + 6;
        var overUnder = tricksTaken - requiredTricks;

        if (overUnder < 0)
            return -UndertrickPenalty(Math.Abs(overUnder), vulnerable, doubled, redoubled);

        // Made the contract
        var trickScore = TrickScore(level, strain, doubled, redoubled);
        var isGame = trickScore >= 100;
        var isSmallSlam = level == 6;
        var isGrandSlam = level == 7;

        var score = trickScore;

        // Bonuses
        if (isGrandSlam)
            score += vulnerable ? 1500 : 1000;
        else if (isSmallSlam)
            score += vulnerable ? 750 : 500;

        if (isGame)
            score += vulnerable ? 500 : 300;
        else
            score += 50; // part score bonus

        // Insult bonus for making doubled/redoubled
        if (redoubled) score += 100;
        else if (doubled) score += 50;

        // Overtricks
        if (overUnder > 0)
            score += OvertrickValue(overUnder, strain, vulnerable, doubled, redoubled);

        return score;
    }

    private static int TrickScore(int level, Strain strain, bool doubled, bool redoubled)
    {
        int perTrick = strain switch
        {
            Strain.Clubs or Strain.Diamonds => 20,
            Strain.Hearts or Strain.Spades => 30,
            Strain.NoTrump => 30, // first trick is 40, handled below
            _ => 30
        };

        var baseScore = perTrick * level;
        if (strain == Strain.NoTrump)
            baseScore += 10; // extra 10 for the first NT trick (40 - 30)

        if (redoubled) return baseScore * 4;
        if (doubled) return baseScore * 2;
        return baseScore;
    }

    private static int OvertrickValue(int overtricks, Strain strain, bool vulnerable,
        bool doubled, bool redoubled)
    {
        if (redoubled)
            return overtricks * (vulnerable ? 400 : 200);
        if (doubled)
            return overtricks * (vulnerable ? 200 : 100);

        // Undoubled overtricks at trick value
        var perTrick = strain switch
        {
            Strain.Clubs or Strain.Diamonds => 20,
            _ => 30
        };
        return overtricks * perTrick;
    }

    private static int UndertrickPenalty(int undertricks, bool vulnerable, bool doubled, bool redoubled)
    {
        if (!doubled && !redoubled)
            return undertricks * (vulnerable ? 100 : 50);

        var multiplier = redoubled ? 2 : 1;

        if (!vulnerable)
        {
            // Non-vul doubled: 100, 200, 200, 300, 300, ...
            // i.e. first=100, second+third=200 each, fourth+=300 each
            var penalty = 0;
            for (var i = 1; i <= undertricks; i++)
            {
                penalty += i switch
                {
                    1 => 100,
                    <= 3 => 200,
                    _ => 300
                };
            }
            return penalty * multiplier;
        }
        else
        {
            // Vul doubled: 200, 300, 300, 300, ...
            var penalty = 0;
            for (var i = 1; i <= undertricks; i++)
            {
                penalty += i == 1 ? 200 : 300;
            }
            return penalty * multiplier;
        }
    }

    public static (int Level, Strain Strain, bool Doubled, bool Redoubled)? ParseContract(string contract)
    {
        if (string.IsNullOrEmpty(contract) || contract == "Pass") return null;
        if (!char.IsDigit(contract[0])) return null;

        var level = contract[0] - '0';
        if (level < 1 || level > 7) return null;

        var rest = contract[1..];
        var redoubled = rest.EndsWith("XX");
        if (redoubled) rest = rest[..^2];
        var doubled = !redoubled && rest.EndsWith("X");
        if (doubled) rest = rest[..^1];

        var strain = rest.ToUpper() switch
        {
            "C" => Strain.Clubs,
            "D" => Strain.Diamonds,
            "H" => Strain.Hearts,
            "S" => Strain.Spades,
            "NT" or "N" => Strain.NoTrump,
            _ => (Strain?)null
        };

        if (strain == null) return null;
        return (level, strain.Value, doubled, redoubled);
    }

    public enum Strain { Clubs, Diamonds, Hearts, Spades, NoTrump }
}
