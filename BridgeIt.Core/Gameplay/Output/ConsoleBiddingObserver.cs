using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public void OnBid(Seat seat, Bid bid)
    {
        Console.WriteLine($"{seat}: {bid}");
    }
}