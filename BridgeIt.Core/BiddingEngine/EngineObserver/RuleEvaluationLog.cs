namespace BridgeIt.Core.BiddingEngine.EngineObserver;

/// <summary>
/// Full evaluation trace for a single bid decision, sent to the UI for debugging.
/// </summary>
public class RuleEvaluationLog
{
    public string Seat { get; set; } = "";
    public string Hand { get; set; } = "";
    public int Hcp { get; set; }
    public bool IsBalanced { get; set; }
    public Dictionary<string, int> Shape { get; set; } = new();
    public string SeatRole { get; set; } = "";
    public string AuctionPhase { get; set; } = "";
    public int BiddingRound { get; set; }
    public string PartnerLastBid { get; set; } = "—";
    public Dictionary<string, TableKnowledgeEntry> TableKnowledge { get; set; } = new();
    public string WinningBid { get; set; } = "Pass";
    public List<RuleEvaluation> EvaluatedRules { get; set; } = new();
}

/// <summary>
/// One rule's evaluation result within a bid decision.
/// </summary>
public class RuleEvaluation
{
    public string RuleName { get; set; } = "";
    public int Priority { get; set; }
    public bool IsApplicableToAuction { get; set; }
    public bool? IsHandApplicable { get; set; }
    public string? ProducedBid { get; set; }
    public bool WasSelected { get; set; }
    public bool WasInvalidBid { get; set; }
    public List<ConstraintDetail>? ForwardConstraints { get; set; }
    public List<ConstraintEvalResult>? ConstraintResults { get; set; }
}

/// <summary>
/// Serialized form of any IBidConstraint for the UI.
/// </summary>
public class ConstraintDetail
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? Suit { get; set; }
    public List<ConstraintDetail>? Children { get; set; }
}

/// <summary>
/// A constraint evaluation result: what the rule required vs what the hand has.
/// </summary>
public class ConstraintEvalResult
{
    public ConstraintDetail Constraint { get; set; } = new();
    public bool Passed { get; set; }
    public string? ActualValue { get; set; }
}

/// <summary>
/// Per-seat knowledge snapshot for serialization.
/// </summary>
public class TableKnowledgeEntry
{
    public int HcpMin { get; set; }
    public int HcpMax { get; set; }
    public bool IsBalanced { get; set; }
    public Dictionary<string, int> MinShape { get; set; } = new();
    public Dictionary<string, int> MaxShape { get; set; } = new();
}
