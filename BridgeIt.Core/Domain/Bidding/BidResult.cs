namespace BridgeIt.Core.Domain.Bidding;

/// <summary>
/// Pairs a bid with whether it should be alerted (conventional/artificial).
/// Returned by the bidding engine and player interface so alert status
/// flows through to the auction history and UI.
/// </summary>
public record BidResult(Bid Bid, bool IsAlerted = false);
