namespace BridgeIt.Dds.Models;

public enum ParType
{
    Make,
    Sacrifice
}

/// <summary>
/// Par result for a specific vulnerability: the optimal contract and score
/// assuming perfect bidding by both sides.
/// </summary>
public class ParResult
{
    public ParType Type { get; set; }
    public string Contract { get; set; } = "";
    public bool Doubled { get; set; }
    public string Declarer { get; set; } = "";
    public int Tricks { get; set; }
    public int? UnderTricks { get; set; }
    public int Score { get; set; }
    public string ScoringSide { get; set; } = "";
    public ParMakeResult? NsBestMake { get; set; }
}

/// <summary>
/// When par is a sacrifice, this holds the best make available to N/S.
/// </summary>
public class ParMakeResult
{
    public string Contract { get; set; } = "";
    public string Declarer { get; set; } = "";
    public int Score { get; set; }
}
