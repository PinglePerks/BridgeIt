using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

/// <summary>
/// Constraint that checks the hand has at most <see cref="MaxLength"/> cards in a suit.
/// Used for takeout double inference ("short in opponent's suit").
/// </summary>
public class ShortageConstraint : IBidConstraint
{
    public Suit Suit { get; }
    public int MaxLength { get; }

    public ShortageConstraint(Suit suit, int maxLength = 2)
    {
        Suit = suit;
        MaxLength = maxLength;
    }

    public bool IsMet(DecisionContext ctx)
    {
        ctx.HandEvaluation.Shape.TryGetValue(Suit, out var count);
        return count <= MaxLength;
    }
}
