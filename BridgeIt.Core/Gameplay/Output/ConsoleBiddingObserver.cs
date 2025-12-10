using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public void OnBid(Seat seat, Bid decision)
    {
        Console.WriteLine($"{seat, -7}: {decision.ToString(),-5}" );

    }
    
}