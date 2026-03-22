using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Domain.IBidValidityChecker;

public interface IBidValidityChecker
{
    bool IsValid(AuctionBid bid, AuctionHistory auctionHistory);
}