namespace BridgeIt.Systems.Config;

/// <summary>
/// Configuration for responses to 1-level suit openings.
/// </summary>
public record ResponseTo1SuitConfig
{
    /// <summary>Minimum HCP for a new suit response at the 1-level. Typically 6.</summary>
    public int MinHcpFor1Level { get; init; }

    /// <summary>Minimum HCP for a new suit response at the 2-level. Typically 10.</summary>
    public int MinHcpFor2Level { get; init; }

    /// <summary>Maximum HCP for a simple raise (2-level). Typically 9.</summary>
    public int RaiseTo2MaxHcp { get; init; }

    /// <summary>Maximum HCP for a limit raise (3-level). Typically 12.</summary>
    public int RaiseTo3MaxHcp { get; init; }

    /// <summary>Minimum HCP for a game-forcing response. Typically 13.</summary>
    public int GameForceMinHcp { get; init; }

    /// <summary>Minimum HCP for a 1NT response. Typically 6.</summary>
    public int NTResponseMinHcp { get; init; }

    /// <summary>Maximum HCP for a 1NT response. Typically 9.</summary>
    public int NTResponseMaxHcp { get; init; }

    /// <summary>Whether splinter bids are played in response to 1-suit openings.</summary>
    public bool SplinterBids { get; init; }
}

/// <summary>
/// Configuration for responses to a strong 2C opening.
/// </summary>
public record ResponseTo2CConfig
{
    public bool Enabled { get; init; }

    /// <summary>The negative response bid. "2D" for standard, "2H" for Benji.</summary>
    public string NegativeResponseBid { get; init; } = "2D";
}
