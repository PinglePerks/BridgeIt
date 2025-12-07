using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Gameplay.Table;

public class StandardAuctionRules : IAuctionRules
{
    public bool ShouldStop(IReadOnlyList<AuctionBid> bids)
    {
        if (bids.Count < 4)
            return false;
        if (bids[^1].Decision.ChosenBid.Type == BidType.NoTrumps && bids[^1].Decision.ChosenBid.Level == 7) 
            return true;
        return bids.TakeLast(3).All(b => b.Decision.ChosenBid.Type == BidType.Pass);
    }
    
}