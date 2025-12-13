using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hands;

public static class ShapeEvaluator
{
    public static Dictionary<Suit, int> GetShape(Domain.Primatives.Hand hand)
    {
        var shape = new Dictionary<Suit, int>()
        {
            
        };
        
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            shape.Add(suit, hand.CountSuit(suit));
        }

        return shape;
    }

    public static bool IsBalanced(Domain.Primatives.Hand hand)
    {
        var shape = GetShape(hand).Values.OrderByDescending(x => x).ToArray();

        return shape.SequenceEqual([4, 3, 3, 3]) ||
               shape.SequenceEqual([4, 4, 3, 2]) ||
               shape.SequenceEqual([5, 3, 3, 2]);
    }

    public static bool IsSemiBalanced(Domain.Primatives.Hand hand)
    {
        // Often: 5-4-2-2 or 6-3-2-2
        var shape = GetShape(hand).Values.OrderByDescending(x => x).ToArray();

        return shape.SequenceEqual([5, 4, 2, 2]) ||
               shape.SequenceEqual([6, 3, 2, 2]);
    }
}