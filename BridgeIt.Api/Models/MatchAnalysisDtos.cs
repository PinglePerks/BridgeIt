namespace BridgeIt.Api.Models;

public record MatchDto(
    string MatchId,
    string Filename,
    int BoardCount,
    List<BoardDto> Boards
);

public record BoardDto(
    string BoardNumber,
    string Vulnerability,
    string Dealer,
    Dictionary<string, HandDto> Hands,
    Dictionary<string, string> PlayerNames,
    DdsTableDto? DdsTable,
    string? ParContract,
    int? ParScore,
    string? ParDeclarer,
    List<string> PlayedAuction,
    string? PlayedContract,
    string? PlayedDeclarer,
    int? PlayedTricks,
    string? PlayedResultDisplay,
    int? PlayedScore,
    string? OurSeat1,
    string? OurSeat2
);

public record HandDto(
    string Display,
    int Hcp,
    List<SuitHoldingDto> Suits
);

public record SuitHoldingDto(
    string Suit,
    string Symbol,
    string Cards,
    string Color
);

public record DdsTableDto(
    Dictionary<string, Dictionary<string, int>> Tricks
);

public record EngineAuctionDto(
    List<EngineBidDto> Bids,
    string? FinalContract,
    string? Declarer,
    int? EngineScore,
    int? EngineTricks,
    string? EngineResultDisplay
);

public record EngineBidDto(
    string Seat,
    string Call,
    string? RuleName,
    int? Priority,
    string? Explanation,
    bool IsAlerted
);

public record MatchBoardResultDto(
    string BoardNumber,
    string? PlayedContract,
    string? EngineContract,
    string? ParContract,
    string Verdict,
    int? ScoreDiff
);

public record EngineAuctionDetailDto(
    List<EngineBidDto> Bids,
    string? FinalContract,
    string? Declarer,
    int? EngineScore,
    int? EngineTricks,
    string? EngineResultDisplay,
    List<RuleEvaluationLogDto> DebugLogs
);

public record RuleEvaluationLogDto(
    string Seat,
    string Hand,
    int Hcp,
    bool IsBalanced,
    Dictionary<string, int> Shape,
    string SeatRole,
    string AuctionPhase,
    int BiddingRound,
    string PartnerLastBid,
    Dictionary<string, TableKnowledgeEntryDto> TableKnowledge,
    string WinningBid,
    List<RuleEvaluationDto> EvaluatedRules
);

public record RuleEvaluationDto(
    string RuleName,
    int Priority,
    bool IsApplicableToAuction,
    bool? IsHandApplicable,
    string? ProducedBid,
    bool WasSelected,
    bool WasInvalidBid,
    List<ConstraintDetailDto>? ForwardConstraints,
    List<ConstraintEvalResultDto>? ConstraintResults
);

public record ConstraintDetailDto(
    string Type,
    string Description,
    int? Min,
    int? Max,
    string? Suit,
    List<ConstraintDetailDto>? Children
);

public record ConstraintEvalResultDto(
    ConstraintDetailDto Constraint,
    bool Passed,
    string? ActualValue
);

public record TableKnowledgeEntryDto(
    int HcpMin,
    int HcpMax,
    bool IsBalanced,
    Dictionary<string, int> MinShape,
    Dictionary<string, int> MaxShape
);

public record MaxMakeableDto(
    string? Strain,       // "clubs", "diamonds", "hearts", "spades", "notrump"
    int? Level,           // 1–7
    string? Declarer,     // "N", "E", "S", "W"
    int? Tricks           // raw trick count (7–13)
);

public record MaxMakeableAnalysisDto(
    MaxMakeableDto? NorthSouth,
    MaxMakeableDto? EastWest
);

// --- Partnership Simulation DTOs ---

public record PartnershipSimulationDto(
    string? OurSeat1,
    string? OurSeat2,
    List<SimulatedBidDto> Bids,
    string? FinalContract,
    string? Declarer,
    int? SimulatedScore,
    int? SimulatedTricks,
    string? SimulatedResultDisplay,
    List<RuleEvaluationLogDto> DebugLogs,
    List<ConflictNoteDto> Conflicts,
    string? IdentificationFailureReason
);

public record SimulatedBidDto(
    string Seat,
    string Call,
    string Source,
    string? RuleName,
    string? Explanation
);

public record ConflictNoteDto(
    string Seat,
    string RealBid,
    string Reason
);
