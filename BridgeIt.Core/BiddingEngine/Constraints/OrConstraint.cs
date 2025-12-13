using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class OrConstraint : IBidConstraint
{
    private readonly List<IBidConstraint> _constraints = new();
    
    public void Add(IBidConstraint constraint) => _constraints.Add(constraint);

    public bool IsMet(DecisionContext ctx)
    {
        return _constraints.Any(c => c.IsMet(ctx));
    }
}