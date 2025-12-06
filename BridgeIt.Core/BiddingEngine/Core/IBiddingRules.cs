using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Core;

public interface IBiddingRule
{
    string Name { get; }
    int Priority { get; }
    bool IsApplicable(BiddingContext ctx);
    BiddingDecision? Apply(BiddingContext ctx);
}