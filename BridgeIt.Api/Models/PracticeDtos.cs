namespace BridgeIt.Api.Models;

public record PracticeConfigDto(
    int HostSeat,
    string Situation,
    string[] Conventions,
    int? HandLimit
);

public record PracticeSessionInfo(
    string SessionId,
    int HostSeat,
    int GuestSeat,
    string Situation,
    string[] Conventions,
    int? HandLimit,
    bool GuestConnected
);

public record HandResultDto(
    string YourContract,
    int YourScore,
    string OptimumContract,
    int OptimumScore,
    int Delta,
    string Verdict
);

public record PracticeHandSummary(
    int HandNumber,
    string ContractBid,
    string OptimumContract,
    int Delta
);
