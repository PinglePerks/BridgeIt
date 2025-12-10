using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public interface IBiddingObserver
{
    void OnBid(Seat seat, Bid decision);
}