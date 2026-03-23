using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

/// <summary>
/// Negative inference: the player does NOT satisfy all of these constraints
/// simultaneously. Created when a player passes and a rule that requires
/// these constraints was applicable but didn't fire.
///
/// For example, NOT(HCP >= 6 AND hearts >= 4) means EITHER HCP &lt; 6 OR hearts &lt; 4.
///
/// Resolution in PlayerKnowledgeEvaluator:
///   When all components except one are already satisfied by positive knowledge,
///   the remaining component must be false — giving a concrete negative inference.
///   E.g. if we already know HCP >= 6 from a prior bid, then hearts &lt; 4 is forced.
/// </summary>
public class NegatedCompositeConstraint : IBidConstraint
{
    public readonly List<IBidConstraint> Components = new();

    public void Add(IBidConstraint constraint) => Components.Add(constraint);

    /// <summary>
    /// Forward check: true when NOT all components are met (i.e. the negation holds).
    /// </summary>
    public bool IsMet(DecisionContext ctx)
    {
        return !Components.All(c => c.IsMet(ctx));
    }
}
