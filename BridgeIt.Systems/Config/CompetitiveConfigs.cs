namespace BridgeIt.Systems.Config;

/// <summary>
/// Configuration for overcalls. Unbuilt — loader will skip.
/// </summary>
public record OvercallConfig
{
    public SimpleOvercallConfig? Simple { get; init; }
    public JumpOvercallConfig? Jump { get; init; }
    public TakeoutDoubleConfig? TakeoutDouble { get; init; }
    public CueBidOvercallConfig? CueBid { get; init; }
    public NTOvercallConfig? NT1 { get; init; }
    public UnusualNTConfig? Unusual2NT { get; init; }
}

/// <summary>
/// Takeout double configuration.
/// </summary>
public record TakeoutDoubleConfig
{
    /// <summary>Minimum HCP for a classic shape takeout double.</summary>
    public int MinHcp { get; init; }

    /// <summary>HCP threshold above which any shape qualifies (too strong to overcall).</summary>
    public int StrongOverrideHcp { get; init; }
}

public record SimpleOvercallConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; } = 15;
}

/// <summary>
/// Jump overcall configuration.
/// Foundation/L2: Intermediate (12-16, good 6-card suit).
/// Benji: Weak (usually 6-card suit).
/// </summary>
public record JumpOvercallConfig
{
    /// <summary>"Intermediate" or "Weak".</summary>
    public string Style { get; init; } = "Intermediate";
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }
    public int MinSuitLength { get; init; }
}

/// <summary>
/// Cue bid overcall configuration (Michaels).
/// </summary>
public record CueBidOvercallConfig
{
    public bool Enabled { get; init; }

    /// <summary>"Michaels" or other style name.</summary>
    public string Style { get; init; } = "Michaels";
    public int MinSuitLength { get; init; }
}

/// <summary>
/// 1NT overcall configuration.
/// </summary>
public record NTOvercallConfig
{
    public int DirectMinHcp { get; init; }
    public int DirectMaxHcp { get; init; }
    public int ProtectiveMinHcp { get; init; }
    public int ProtectiveMaxHcp { get; init; }
}

/// <summary>
/// Unusual 2NT overcall (shows two lowest unbid suits).
/// </summary>
public record UnusualNTConfig
{
    public bool Enabled { get; init; }
}

/// <summary>
/// Negative double configuration.
/// </summary>
public record NegativeDoubleConfig
{
    public bool Enabled { get; init; }

    /// <summary>Maximum level to which negative doubles apply. e.g. "2S" or "3S".</summary>
    public string MaxLevel { get; init; } = "2S";
}

/// <summary>
/// Competitive auction agreements.
/// </summary>
public record CompetitiveAuctionConfig
{
    public bool RedoubleShows9Plus { get; init; }
    public bool NewSuitForcing { get; init; }
    public bool JumpRaisePreemptive { get; init; }
    public bool TwoNTGoodRaise { get; init; }
    public UnassumingCueBidConfig? UnassumingCueBids { get; init; }
}

/// <summary>
/// Unassuming cue bid configuration (opposite partner's overcall, shows a good raise).
/// </summary>
public record UnassumingCueBidConfig
{
    public bool Enabled { get; init; }
}
