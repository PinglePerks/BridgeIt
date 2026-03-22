using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Partnership;

/// <summary>
/// Builds a PlayerKnowledge from a list of BidInformation constraints.
/// Generic — works equally for partner, opponents, or any player.
/// </summary>
public static class PlayerKnowledgeEvaluator
{
    public static PlayerKnowledge AnalyzeKnowledge(List<BidInformation> bidInfos)
    {
        var knowledge = new PlayerKnowledge();

        foreach (var info in bidInfos)
        {
            if (info.Constraint != null)
            {
                ExtractKnowledgeFromConstraint(info.Constraint, knowledge);
            }
        }

        return knowledge;
    }

    internal static void ExtractKnowledgeFromConstraint(IBidConstraint constraint, PlayerKnowledge knowledge)
    {
        switch (constraint)
        {
            case CompositeConstraint composite:
                foreach (var child in composite.Constraints)
                {
                    ExtractKnowledgeFromConstraint(child, knowledge);
                }
                break;

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
        }
    }
}
