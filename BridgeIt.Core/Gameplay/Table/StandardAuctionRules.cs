using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Gameplay.Table;

public class StandardAuctionRules : IAuctionRules
{
    public bool ShouldStop(IReadOnlyList<BiddingDecision> bids)
    {
        if (bids.Count < 4)
            return false;
        if (bids[^1].ChosenBid.Type == BidType.NoTrumps && bids[^1].ChosenBid.Level == 7) 
            return true;
        return bids.TakeLast(3).All(b => b.ChosenBid.Type == BidType.Pass);
    }
    
}