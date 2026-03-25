namespace BridgeIt.Systems.Config;

/// <summary>
/// Configuration for opener's rebids.
/// </summary>
public record OpenerRebidConfig
{
    /// <summary>Minimum HCP for a reverse bid. Typically 16.</summary>
    public int ReverseMinHcp { get; init; }

    /// <summary>NT rebid HCP ranges after a 1-level response.</summary>
    public NTRebidRangeConfig? NTRebid { get; init; }
}

/// <summary>
/// HCP ranges for opener's NT rebids.
/// Foundation/L2: 15-16 / 17-18 / 19.
/// Benji: 15-17 / 18-19 / long suit.
/// </summary>
public record NTRebidRangeConfig
{
    public int Rebid1NTMin { get; init; }
    public int Rebid1NTMax { get; init; }
    public int Rebid2NTMin { get; init; }
    public int Rebid2NTMax { get; init; }
    public int Rebid3NTMin { get; init; }
    public int Rebid3NTMax { get; init; }
}

/// <summary>
/// Configuration for responder's rebids. Placeholder — expand as rules grow.
/// </summary>
public record ResponderRebidConfig;
