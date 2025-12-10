using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public interface IBidConstraint
{
    bool IsMet(DecisionContext ctx);
}