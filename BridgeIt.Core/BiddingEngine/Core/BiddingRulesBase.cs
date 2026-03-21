using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Core;

public abstract class BiddingRuleBase : IBiddingRule
{
    public abstract string Name { get; }
    public abstract int Priority { get; }
    public abstract bool CouldMakeBid(DecisionContext ctx);
    public abstract BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx);
    public abstract Bid? Apply(DecisionContext ctx);
    public abstract bool CouldExplainBid(Bid bid, DecisionContext ctx);
    
    protected int GetNextSuitBidLevel(Suit suit, Bid? currentContract) 
        => Bid.NextLevelForSuit(suit, currentContract);

    protected int GetNextNtBidLevel(Bid? currentContract) 
        => Bid.NextLevelForNoTrumps(currentContract);
}
