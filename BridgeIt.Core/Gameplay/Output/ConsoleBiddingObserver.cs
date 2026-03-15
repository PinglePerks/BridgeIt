using BridgeIt.Core.Analysis.Auction;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public void OnBid(AuctionHistory auctionHistory)
    {
        var lastBid = auctionHistory.Bids[^1];
        Console.WriteLine($"Bid {lastBid.Bid,-8} Seat {lastBid.Seat}");
    }
}