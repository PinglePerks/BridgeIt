using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Core.Gameplay.Output;

public interface IBiddingObserver
{
    void OnBid(AuctionHistory auctionHistory);
}

