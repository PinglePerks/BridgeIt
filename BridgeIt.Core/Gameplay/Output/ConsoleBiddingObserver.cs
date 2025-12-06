using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public sealed class ConsoleBiddingObserver : IBiddingObserver
{
    public void OnBid(Seat seat, BiddingDecision decision)
    {
        Console.WriteLine($"{seat, -7}: {decision.ChosenBid,-5} | {decision.Explanation}");

    }
}