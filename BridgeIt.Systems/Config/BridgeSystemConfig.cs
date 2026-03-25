namespace BridgeIt.Systems.Config;

/// <summary>
/// Top-level configuration for a complete bidding system.
/// Mirrors the sections of an EBU convention card.
/// Nullable sections mean "not configured / no rules for this area".
/// The loader skips sections that don't have corresponding rule implementations.
/// </summary>
public record BridgeSystemConfig
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    // Opening bids
    public NTOpeningConfig? NT1Opening { get; init; }
    public NTOpeningConfig? NT2Opening { get; init; }
    public SuitOpeningConfig? SuitOpening { get; init; }
    public StrongOpeningConfig? StrongOpening { get; init; }
    public PreemptConfig? Preempts { get; init; }
    public TwoLevelOpeningConfig? TwoLevelOpenings { get; init; }

    // Responses to NT openings
    public NTResponseConfig? ResponseTo1NT { get; init; }
    public NTResponseConfig? ResponseTo2NT { get; init; }

    // Responses to suit openings
    public ResponseTo1SuitConfig? ResponseTo1Suit { get; init; }
    public ResponseTo2CConfig? ResponseTo2C { get; init; }

    // Rebids
    public OpenerRebidConfig? OpenerRebid { get; init; }
    public ResponderRebidConfig? ResponderRebid { get; init; }

    // Competitive bidding (unbuilt — loader will skip)
    public OvercallConfig? Overcalls { get; init; }
    public NegativeDoubleConfig? NegativeDoubles { get; init; }
    public CompetitiveAuctionConfig? CompetitiveAuctions { get; init; }

    // Slam (unbuilt — loader will skip)
    public SlamConfig? Slam { get; init; }

    // Convention toggles
    public FourthSuitForcingConfig? FourthSuitForcing { get; init; }
    public SplinterConfig? Splinters { get; init; }
    public TrialBidConfig? TrialBids { get; init; }

    // Rule priorities — flat map of rule-name → priority
    public Dictionary<string, int>? Priorities { get; init; }
}
