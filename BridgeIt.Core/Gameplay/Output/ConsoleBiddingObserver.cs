using BridgeIt.Core.Analysis.Auction;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public Task OnBid(AuctionHistory auctionHistory)
    {
        var lastBid = auctionHistory.Bids[^1];
        var alert = lastBid.IsAlerted ? " (ALERT)" : "";
        Console.WriteLine($"Bid {lastBid.Bid,-8} Seat {lastBid.Seat}{alert}");
        return Task.CompletedTask;
    }
}