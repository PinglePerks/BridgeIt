namespace BridgeIt.Systems.Config;

/// <summary>
/// Configuration for a no-trumps opening bid (1NT or 2NT).
/// </summary>
public record NTOpeningConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }
    public int Level { get; init; }
    public bool AllowSingleton { get; init; }
}

/// <summary>
/// Configuration for 1-level suit openings.
/// Handles both 4-card (Acol) and 5-card (SAYC) major systems.
/// </summary>
public record SuitOpeningConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }

    /// <summary>Minimum length for a major suit opening. 4 for Acol, 5 for SAYC.</summary>
    public int MajorMinLength { get; init; }

    /// <summary>Minimum length for a minor suit opening. 4 for Acol, 3 for SAYC.</summary>
    public int MinorMinLength { get; init; }

    /// <summary>"HigherRanking" (Acol: open higher of equal-length suits) or "BetterMinor" (SAYC: open longer minor, 1C with 3-3).</summary>
    public string MinorSelection { get; init; } = "HigherRanking";

    /// <summary>Allow light opening if HCP + length of 2 longest suits >= 20.</summary>
    public bool RuleOf20 { get; init; }

    /// <summary>Acol 4-card major rules (4-4 major opening, 4441 hands). Null for 5-card major systems.</summary>
    public FourCardMajorConfig? FourCardMajor { get; init; }
}

/// <summary>
/// Acol-specific rules for opening with 4-card majors.
/// </summary>
public record FourCardMajorConfig
{
    /// <summary>With 4-4 in both majors, open this bid. "1H" for Acol.</summary>
    public string? FourFourMajorOpening { get; init; }

    /// <summary>4441 hand openings keyed by singleton suit. e.g. {"Clubs":"1H", "Diamonds":"1C", "Hearts":"1D", "Spades":"1D"}.</summary>
    public Dictionary<string, string>? FourFourFourOneMap { get; init; }
}

/// <summary>
/// Configuration for strong artificial opening (2C in most systems).
/// </summary>
public record StrongOpeningConfig
{
    public int MinHcpBalanced { get; init; }
    public int MinHcpUnbalanced { get; init; }
    public int MaxHcp { get; init; }

    /// <summary>The bid used for the strong opening. "2C" for standard and Benji.</summary>
    public string Bid { get; init; } = "2C";

    /// <summary>True if game-forcing (standard 2C), false for Benji's Acol 2.</summary>
    public bool GameForcing { get; init; }
}

/// <summary>
/// Configuration for preemptive openings (3-level and 4-level).
/// </summary>
public record PreemptConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }

    /// <summary>Minimum suit length for a 3-level preempt. 7 for Foundation/L2, 6 for Benji.</summary>
    public int MinSuitLength3Level { get; init; }

    /// <summary>Minimum suit length for a 4-level preempt. 8 for Foundation/L2, 7 for Benji.</summary>
    public int MinSuitLength4Level { get; init; }

    /// <summary>Bids reserved for other conventions (e.g. ["2C"] for strong opening). Preempts won't use these.</summary>
    public List<string>? ReservedBids { get; init; }
}

/// <summary>
/// Configuration for the 2-level opening area (highly variant across systems).
/// </summary>
public record TwoLevelOpeningConfig
{
    /// <summary>Benjamin system: 2C = Acol 2 (strong, not GF), 2D = GF. Swaps meaning of 2C/2D.</summary>
    public bool BenjaminTwos { get; init; }

    /// <summary>Weak two openings (2H, 2S, optionally 2D).</summary>
    public WeakTwoConfig? WeakTwos { get; init; }

    /// <summary>Acol strong twos (2D, 2H, 2S — strong but not game-forcing).</summary>
    public AcolTwoConfig? AcolTwos { get; init; }
}

/// <summary>
/// Configuration for weak two openings.
/// </summary>
public record WeakTwoConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }
    public int MinSuitLength { get; init; }

    /// <summary>Which suits can be opened as weak twos. e.g. ["H","S"] or ["D","H","S"].</summary>
    public List<string> Suits { get; init; } = new();

    /// <summary>Whether 2NT is used as a forcing enquiry (asking for a feature).</summary>
    public bool TwoNTEnquiry { get; init; }
}

/// <summary>
/// Configuration for Acol strong two openings (not game-forcing).
/// </summary>
public record AcolTwoConfig
{
    public int MinHcp { get; init; }
    public int MaxHcp { get; init; }

    /// <summary>Which suits can be opened as Acol twos. e.g. ["D","H","S"].</summary>
    public List<string> Suits { get; init; } = new();
}
