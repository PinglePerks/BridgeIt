using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Core;

public interface IBiddingRule
{
    string Name { get; }
    int Priority { get; }
    bool IsApplicable(DecisionContext ctx);
    Bid? Apply(DecisionContext ctx);
    BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx);
    
}