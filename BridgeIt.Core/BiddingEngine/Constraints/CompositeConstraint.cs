using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class CompositeConstraint : IBidConstraint
{
    public readonly List<IBidConstraint> Constraints = new();

    public void Add(IBidConstraint constraint) => Constraints.Add(constraint);

    public bool IsMet(DecisionContext ctx)
    {
        return Constraints.All(c => c.IsMet(ctx));
    }
}