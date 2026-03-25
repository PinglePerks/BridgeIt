namespace BridgeIt.Systems.Config;

/// <summary>
/// Configuration for responses to an NT opening (1NT or 2NT).
/// Each sub-convention is nullable — null means not played.
/// </summary>
public record NTResponseConfig
{
    public StaymanConfig? Stayman { get; init; }
    public TransferConfig? Transfers { get; init; }
    public MinorTransferConfig? MinorTransfers { get; init; }
    public BaronConfig? Baron { get; init; }
    public WeaknessTakeoutConfig? WeaknessTakeouts { get; init; }
    public NTRaiseConfig? Raise { get; init; }
    public Jacoby2NTConfig? Jacoby2NT { get; init; }

    /// <summary>After opponents double: all 2-level responses become natural.</summary>
    public bool NaturalAfterDouble { get; init; }
}

/// <summary>
/// Stayman convention configuration.
/// </summary>
public record StaymanConfig
{
    public bool Enabled { get; init; }

    /// <summary>Minimum HCP to use Stayman. 11 after 1NT, 4 after 2NT, 0 after 2C-2D-2NT.</summary>
    public int MinHcp { get; init; }
}

/// <summary>
/// Jacoby transfer configuration (major suits only).
/// </summary>
public record TransferConfig
{
    public bool Enabled { get; init; }
    public int MinSuitLength { get; init; }
    public int MaxSuitLength { get; init; }
}

/// <summary>
/// Minor suit transfer configuration.
/// Benji: 2S transfers to clubs, 2NT transfers to diamonds.
/// Level 2: 2NT relays to 3C (pass or correct to 3D).
/// </summary>
public record MinorTransferConfig
{
    public bool Enabled { get; init; }

    /// <summary>Bid that transfers to clubs. e.g. "2S" (Benji after 1NT).</summary>
    public string? ClubTransferBid { get; init; }

    /// <summary>Bid that transfers to diamonds. e.g. "2NT" (Benji after 1NT).</summary>
    public string? DiamondTransferBid { get; init; }
}

/// <summary>
/// Baron convention configuration.
/// Level 2: 2S over 1NT or 3S over 2NT — shows invitational or GF with slam interest.
/// </summary>
public record BaronConfig
{
    public bool Enabled { get; init; }

    /// <summary>The bid used for Baron. "2S" after 1NT, "3S" after 2NT.</summary>
    public string? Bid { get; init; }

    public int InviteMinHcp { get; init; }
    public int InviteMaxHcp { get; init; }
}

/// <summary>
/// Weakness takeout configuration (Foundation Acol: 2D/2H/2S are drop-dead bids, no transfer meaning).
/// </summary>
public record WeaknessTakeoutConfig
{
    public bool Enabled { get; init; }

    /// <summary>Bids that are weakness takeouts. e.g. ["2D","2H","2S"].</summary>
    public List<string> Bids { get; init; } = new();
}

/// <summary>
/// NT raise configuration (invitational raise).
/// </summary>
public record NTRaiseConfig
{
    public bool Enabled { get; init; }
    public int InviteMinHcp { get; init; }
    public int InviteMaxHcp { get; init; }
}

/// <summary>
/// Jacoby 2NT convention (game-forcing raise of partner's major).
/// </summary>
public record Jacoby2NTConfig
{
    public bool Enabled { get; init; }
    public int MinHcp { get; init; }
    public int MinFitLength { get; init; }
}
