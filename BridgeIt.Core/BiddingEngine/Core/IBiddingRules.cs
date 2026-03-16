using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Core;

public interface IBiddingRule
{
    //Metadata
    string Name { get; }
    int Priority { get; }
    
    //Forward solving
    bool IsApplicable(DecisionContext ctx);
    Bid? Apply(DecisionContext ctx);
    
    //Backward solving
    BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx);
}