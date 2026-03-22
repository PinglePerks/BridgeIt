using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hands;

public static class ShapeEvaluator
{
    public static Dictionary<Suit, int> GetShape(Hand hand)
    {
        var shape = new Dictionary<Suit, int>();
        
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

    /// <summary>
    /// Returns the single longest suit, tie-broken by highest ranking.
    /// Suitable as a general-purpose summary; rules should use
    /// SuitsWithMinLength / SuitsByLengthDescending for more nuanced selection.
    /// </summary>
    public static Suit LongestAndStrongest(Hand hand)
    {
        return GetShape(hand)
            .OrderByDescending(s => s.Value)
            .ThenByDescending(s => s.Key)
            .First()
            .Key;
    }

    /// <summary>
    /// Returns all suits with at least <paramref name="minLength"/> cards,
    /// ordered by length descending then rank descending.
    /// </summary>
    public static List<Suit> SuitsWithMinLength(Hand hand, int minLength)
    {
        return GetShape(hand)
            .Where(kv => kv.Value >= minLength)
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => kv.Key)
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>
    /// Returns all four suits ordered by length descending then rank descending.
    /// </summary>
    public static List<Suit> SuitsByLengthDescending(Hand hand)
    {
        return GetShape(hand)
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => kv.Key)
            .Select(kv => kv.Key)
            .ToList();
    }

}