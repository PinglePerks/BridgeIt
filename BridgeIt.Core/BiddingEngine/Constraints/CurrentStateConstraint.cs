using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class CurrentStateConstraint : IBidConstraint
{
    private readonly PartnershipBiddingState _currentState;

    public CurrentStateConstraint(PartnershipBiddingState currentState)
    {
        _currentState = currentState;
    }

    public bool IsMet(DecisionContext ctx)
    {
        return ctx.PartnershipKnowledge.PartnershipBiddingState == _currentState;
    }
}