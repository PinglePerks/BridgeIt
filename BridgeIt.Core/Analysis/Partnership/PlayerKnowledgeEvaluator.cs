using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Partnership;

/// <summary>
/// Builds a PlayerKnowledge from a list of BidInformation constraints.
/// Generic — works equally for partner, opponents, or any player.
///
/// Uses two-pass processing:
///   Pass 1: extract all positive constraints (HCP, suit length, balanced)
///   Pass 2: resolve negated constraints against accumulated positive knowledge
/// </summary>
public static class PlayerKnowledgeEvaluator
{
    public static PlayerKnowledge AnalyzeKnowledge(List<BidInformation> bidInfos)
    {
        var knowledge = new PlayerKnowledge();
        var negations = new List<NegatedCompositeConstraint>();

        // Pass 1: extract positive constraints, collect negations for later
        foreach (var info in bidInfos)
        {
            if (info.Constraint != null)
            {
                CollectConstraints(info.Constraint, knowledge, negations);
            }
        }

        // Pass 2: resolve negated constraints using accumulated positive knowledge
        foreach (var negation in negations)
        {
            ResolveNegation(negation, knowledge);
        }

        return knowledge;
    }

    /// <summary>
    /// Walks a constraint tree, applying positive constraints immediately
    /// and deferring negated constraints for resolution in pass 2.
    /// </summary>
    private static void CollectConstraints(
        IBidConstraint constraint,
        PlayerKnowledge knowledge,
        List<NegatedCompositeConstraint> negations)
    {
        switch (constraint)
        {
            case NegatedCompositeConstraint negated:
                negations.Add(negated);
                break;

            case CompositeConstraint composite:
                foreach (var child in composite.Constraints)
                    CollectConstraints(child, knowledge, negations);
                break;

            default:
                ExtractPositiveKnowledge(constraint, knowledge);
                break;
        }
    }

    /// <summary>
    /// Applies a single positive (non-negated) constraint to knowledge.
    /// </summary>
    internal static void ExtractPositiveKnowledge(IBidConstraint constraint, PlayerKnowledge knowledge)
    {
        switch (constraint)
        {
            case HcpConstraint hcpConstraint:
                knowledge.HcpMax = Math.Min(knowledge.HcpMax, hcpConstraint.Max);
                knowledge.HcpMin = Math.Max(knowledge.HcpMin, hcpConstraint.Min);
                break;

            case BalancedConstraint:
                knowledge.IsBalanced = true;
                foreach (Suit s in Enum.GetValues(typeof(Suit)))
                {
                    knowledge.MinShape[s] = Math.Max(2, knowledge.MinShape[s]);
                    knowledge.MaxShape[s] = Math.Min(5, knowledge.MaxShape[s]);
                }
                break;

            case SuitLengthConstraint suitLengthConstraint:
                if (suitLengthConstraint.Suit == null) break;

                knowledge.MinShape[suitLengthConstraint.Suit!.Value] = Math.Max(
                    suitLengthConstraint.MinLen,
                    knowledge.MinShape[suitLengthConstraint.Suit!.Value]);

                knowledge.MaxShape[suitLengthConstraint.Suit!.Value] = Math.Min(
                    suitLengthConstraint.MaxLen,
                    knowledge.MaxShape[suitLengthConstraint.Suit!.Value]);
                break;

            case LosingTrickCountConstraint ltc:
                knowledge.LosersMin = Math.Max(knowledge.LosersMin, ltc.Min);
                knowledge.LosersMax = Math.Min(knowledge.LosersMax, ltc.Max);
                break;
        }
    }

    /// <summary>
    /// Resolves a NegatedCompositeConstraint against current knowledge.
    ///
    /// A negation says "NOT all of [C1, C2, ..., Cn] are true simultaneously."
    /// If all components except one are already satisfied by positive knowledge,
    /// the remaining component must be false — giving a concrete inference.
    ///
    /// Example: NOT(HCP >= 6 AND hearts >= 4). If we already know HCP >= 6
    /// from a prior bid, then hearts &lt; 4 is forced → MaxShape[Hearts] = 3.
    /// </summary>
    internal static void ResolveNegation(NegatedCompositeConstraint negation, PlayerKnowledge knowledge)
    {
        // Find which components are NOT yet confirmed by positive knowledge
        var unsatisfied = new List<IBidConstraint>();

        foreach (var component in negation.Components)
        {
            if (!IsAlreadySatisfied(component, knowledge))
                unsatisfied.Add(component);
        }

        // If exactly one component is unsatisfied, it must be false
        if (unsatisfied.Count == 1)
        {
            ApplyNegation(unsatisfied[0], knowledge);
        }
        // If zero unsatisfied: all components are met but rule didn't fire —
        // this is a contradiction (edge case, ignore safely).
        // If two+ unsatisfied: can't determine which failed, no inference.
    }

    /// <summary>
    /// Checks whether a constraint component is already definitively satisfied
    /// by the current positive knowledge.
    /// </summary>
    private static bool IsAlreadySatisfied(IBidConstraint constraint, PlayerKnowledge knowledge)
    {
        return constraint switch
        {
            HcpConstraint hcp => knowledge.HcpMin >= hcp.Min,
            SuitLengthConstraint suit when suit.Suit.HasValue
                => knowledge.MinShape[suit.Suit.Value] >= suit.MinLen,
            BalancedConstraint => knowledge.IsBalanced,
            LosingTrickCountConstraint ltc => knowledge.LosersMax <= ltc.Max,
            _ => false
        };
    }

    /// <summary>
    /// Applies the negation of a single constraint component:
    ///   HcpConstraint(min, _)        → HcpMax = min(HcpMax, min - 1)
    ///   SuitLengthConstraint(s, min)  → MaxShape[s] = min(MaxShape[s], min - 1)
    /// </summary>
    private static void ApplyNegation(IBidConstraint constraint, PlayerKnowledge knowledge)
    {
        switch (constraint)
        {
            case HcpConstraint hcp:
                knowledge.HcpMax = Math.Min(knowledge.HcpMax, hcp.Min - 1);
                break;

            case SuitLengthConstraint suit when suit.Suit.HasValue && suit.MinLen > 0:
                knowledge.MaxShape[suit.Suit.Value] = Math.Min(
                    knowledge.MaxShape[suit.Suit.Value],
                    suit.MinLen - 1);
                break;

            case LosingTrickCountConstraint ltc when ltc.Max < 13:
                knowledge.LosersMin = Math.Max(knowledge.LosersMin, ltc.Max + 1);
                break;
        }
    }

    // Keep for backward compatibility with any code calling this directly
    internal static void ExtractKnowledgeFromConstraint(IBidConstraint constraint, PlayerKnowledge knowledge)
    {
        switch (constraint)
        {
            case CompositeConstraint composite:
                foreach (var child in composite.Constraints)
                    ExtractKnowledgeFromConstraint(child, knowledge);
                break;
            case NegatedCompositeConstraint:
                // Negated constraints need two-pass resolution; skip in single-pass mode
                break;
            default:
                ExtractPositiveKnowledge(constraint, knowledge);
                break;
        }
    }
}
