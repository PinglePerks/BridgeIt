using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public void OnBid(AuctionHistory auctionHistory)
    {
        Console.WriteLine($"auctionHistory" );

    }
    
}