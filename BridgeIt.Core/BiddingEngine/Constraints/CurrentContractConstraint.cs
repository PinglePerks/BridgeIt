using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class CurrentContractConstraint(string? levelConstraint) : IBidConstraint
{
public bool IsMet(DecisionContext ctx)
    {
        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        if (currentContract == null) return true;
        if (levelConstraint == "1")
        {
            if(currentContract.Level==1 && currentContract.Type != BidType.NoTrumps) return true;
            return false;
        }

        if (levelConstraint == "2")
        {
            return currentContract.Level <= 2 && currentContract != Bid.NoTrumpsBid(2);
        }
        
        if (levelConstraint == "3")
        {
            return currentContract.Level <= 3 && currentContract != Bid.NoTrumpsBid(3);
        }

        return false;
    }
}