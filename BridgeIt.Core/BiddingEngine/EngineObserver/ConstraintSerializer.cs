using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.EngineObserver;

/// <summary>
/// Serializes IBidConstraint instances into ConstraintDetail DTOs for the debug UI.
/// Uses external pattern-matching (visitor) — no changes to the constraint interface needed.
/// </summary>
public static class ConstraintSerializer
{
    public static ConstraintDetail Serialize(IBidConstraint constraint)
    {
        return constraint switch
        {
            HcpConstraint hcp => new ConstraintDetail
            {
                Type = "Hcp",
                Description = $"HCP {hcp.Min}–{hcp.Max}",
                Min = hcp.Min,
                Max = hcp.Max,
            },

            SuitLengthConstraint sl => new ConstraintDetail
            {
                Type = "SuitLength",
                Description = sl.Suit != null
                    ? $"{sl.Suit.Value} {sl.MinLen}–{sl.MaxLen}"
                    : $"Any suit {sl.MinLen}–{sl.MaxLen}",
                Min = sl.MinLen,
                Max = sl.MaxLen,
                Suit = sl.Suit?.ToString(),
            },

            BalancedConstraint => new ConstraintDetail
            {
                Type = "Balanced",
                Description = "Balanced hand",
            },

            LosingTrickCountConstraint ltc => new ConstraintDetail
            {
                Type = "LTC",
                Description = $"LTC {ltc.Min}–{ltc.Max}",
                Min = ltc.Min,
                Max = ltc.Max,
            },

            LongestConstraint lc => new ConstraintDetail
            {
                Type = "Longest",
                Description = $"Longest suit: {lc.Suit}",
                Suit = lc.Suit.ToString(),
            },

            NegatedCompositeConstraint neg => new ConstraintDetail
            {
                Type = "NegatedComposite",
                Description = $"NOT({string.Join(" AND ", neg.Components.Select(c => Serialize(c).Description))})",
                Children = neg.Components.Select(Serialize).ToList(),
            },

            CompositeConstraint comp => new ConstraintDetail
            {
                Type = "Composite",
                Description = string.Join(" AND ", comp.Constraints.Select(c => Serialize(c).Description)),
                Children = comp.Constraints.Select(Serialize).ToList(),
            },

            OrConstraint => new ConstraintDetail
            {
                Type = "Or",
                Description = "One of multiple conditions",
            },

            _ => new ConstraintDetail
            {
                Type = constraint.GetType().Name,
                Description = constraint.GetType().Name,
            },
        };
    }

    public static ConstraintEvalResult Evaluate(IBidConstraint constraint, DecisionContext ctx)
    {
        var detail = Serialize(constraint);
        var passed = constraint.IsMet(ctx);
        var actualValue = GetActualValue(constraint, ctx);

        return new ConstraintEvalResult
        {
            Constraint = detail,
            Passed = passed,
            ActualValue = actualValue,
        };
    }

    public static List<ConstraintEvalResult> EvaluateComposite(CompositeConstraint composite, DecisionContext ctx)
    {
        return composite.Constraints.Select(c => Evaluate(c, ctx)).ToList();
    }

    private static string? GetActualValue(IBidConstraint constraint, DecisionContext ctx)
    {
        return constraint switch
        {
            HcpConstraint => ctx.HandEvaluation.Hcp.ToString(),
            SuitLengthConstraint sl when sl.Suit != null =>
                ctx.HandEvaluation.Shape.TryGetValue(sl.Suit.Value, out var count) ? count.ToString() : "0",
            SuitLengthConstraint => ctx.HandEvaluation.Shape.Values.Max().ToString(),
            BalancedConstraint => ctx.HandEvaluation.IsBalanced.ToString(),
            LosingTrickCountConstraint => ctx.HandEvaluation.Losers.ToString(),
            LongestConstraint => ctx.HandEvaluation.LongestAndStrongest.ToString(),
            _ => null,
        };
    }
}
