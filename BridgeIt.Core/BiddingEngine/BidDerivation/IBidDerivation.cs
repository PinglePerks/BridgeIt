using System.Data;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public interface IBidDerivation
{
    Bid? DeriveBid(DecisionContext ctx);
}

public abstract class BidDerivationBase : IBidDerivation
{
    public abstract Bid? DeriveBid(DecisionContext ctx);
    
    protected int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
    {
        if (currentContract == null) return 1;
        var level = currentContract.Level;
        if (currentContract.Type == BidType.NoTrumps) return level + 1;
        if (suit <= currentContract.Suit) return level + 1;
        return level;
    }

    protected int GetNextNtBidLevel(Bid? currentContract)
    {
        if (currentContract == null) return 1;
        if (currentContract.Type == BidType.NoTrumps) return currentContract.Level + 1;
        return currentContract.Level;
    }
}