using System.Diagnostics.Contracts;
using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class BalancedConstraint : IBidConstraint
{
    public bool IsMet(DecisionContext ctx)
    {
        return ctx.HandEvaluation.IsBalanced;
    }
}