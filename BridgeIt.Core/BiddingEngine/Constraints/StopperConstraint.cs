using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

/// <summary>
/// Constraint that checks whether a hand has the required stopper quality in a given suit.
/// </summary>
public class StopperConstraint : IBidConstraint
{
    public Suit Suit { get; }
    public StopperQuality MinQuality { get; }

    public StopperConstraint(Suit suit, StopperQuality minQuality = StopperQuality.Full)
    {
        Suit = suit;
        MinQuality = minQuality;
    }

    public bool IsMet(DecisionContext ctx)
    {
        if (!ctx.HandEvaluation.SuitStoppers.TryGetValue(Suit, out var quality))
            return false;

        return quality >= MinQuality;
    }
}
