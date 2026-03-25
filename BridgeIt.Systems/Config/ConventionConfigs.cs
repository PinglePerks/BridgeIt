namespace BridgeIt.Systems.Config;

/// <summary>
/// Slam convention configuration.
/// </summary>
public record SlamConfig
{
    /// <summary>"Blackwood", "RKCB", or "None".</summary>
    public string Style { get; init; } = "Blackwood";

    /// <summary>Whether Grand Slam Force (5NT) is played.</summary>
    public bool GrandSlamForce { get; init; }
}

/// <summary>
/// Fourth Suit Forcing convention configuration.
/// </summary>
public record FourthSuitForcingConfig
{
    public bool Enabled { get; init; }
}

/// <summary>
/// Splinter bid configuration.
/// </summary>
public record SplinterConfig
{
    public bool Enabled { get; init; }

    /// <summary>Minimum trump support length for a splinter. Typically 4.</summary>
    public int MinFitLength { get; init; }
}

/// <summary>
/// Trial bid configuration (e.g. long suit trial bids after a simple raise).
/// </summary>
public record TrialBidConfig
{
    public bool Enabled { get; init; }

    /// <summary>"LongSuit" or other style name.</summary>
    public string Style { get; init; } = "LongSuit";
}
