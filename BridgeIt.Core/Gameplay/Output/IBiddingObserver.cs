using BridgeIt.Core.Analysis.Auction;

namespace BridgeIt.Core.Gameplay.Output;

public interface IBiddingObserver
{
    Task OnBid(AuctionHistory auctionHistory);
}

