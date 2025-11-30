using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Gameplay.Table;

public interface IAuctionRules
{
    bool ShouldStop(IReadOnlyList<BiddingDecision> bids);
}