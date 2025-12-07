using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Domain.Bidding;

public record AuctionBid(
    Seat Seat,
    BiddingDecision Decision
    );