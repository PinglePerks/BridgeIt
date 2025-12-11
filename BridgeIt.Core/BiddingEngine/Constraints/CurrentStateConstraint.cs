using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class CurrentStateConstraint : IBidConstraint
{
    private readonly string _currentState;

    public CurrentStateConstraint(string currentState)
    {
        _currentState = currentState;
    }

    public bool IsMet(DecisionContext ctx)
    {
        return ctx.PartnershipKnowledge.CurrentPartnershipState == _currentState;
    }
}