using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class CurrentContractConstraint(string currentContract) : IBidConstraint
{
    private readonly string _currentContract;
    public bool IsMet(BiddingContext ctx)
    {
        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        if (currentContract == null) return true;
        if (_currentContract == "1level")
        {
            if(currentContract.Level==1 && currentContract.Type != BidType.NoTrumps) return true;
            return false;
        }

        if (_currentContract == "2level")
        {
            return currentContract.Level <= 2 && currentContract != Bid.NoTrumpsBid(2);
        }
        
        if (_currentContract == "3level")
        {
            return currentContract.Level <= 3 && currentContract != Bid.NoTrumpsBid(3);
        }

        return false;
    }
}