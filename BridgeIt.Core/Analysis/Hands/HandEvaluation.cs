using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Primatives;
using YamlDotNet.Core.Tokens;

namespace BridgeIt.Core.Analysis.Hands;

public class HandEvaluation
{
    public int Hcp { get; init; }
    public int Losers { get; init; }
    public Dictionary<Suit, int> Shape { get; init; } = new();
    public bool IsBalanced { get; init; }
    public Dictionary<Suit,int> RomanKeyCardCount { get; init; } = new();
    public Suit LongestAndStrongest {get; init;}

    /// <summary>
    /// Returns all suits with at least <paramref name="minLength"/> cards,
    /// ordered by length descending then rank descending.
    /// </summary>
    public List<Suit> SuitsWithMinLength(int minLength)
        => Shape.Where(kv => kv.Value >= minLength)
                .OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key)
                .Select(kv => kv.Key)
                .ToList();

    /// <summary>
    /// Returns all four suits ordered by length descending then rank descending.
    /// </summary>
    public List<Suit> SuitsByLengthDescending()
        => Shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key)
                .Select(kv => kv.Key)
                .ToList();
}

public static class HandEvaluator
{
    public static HandEvaluation Evaluate(Hand hand)
    {
        return new HandEvaluation
        {
            Hcp = HighCardPoints.Count(hand),
            Losers = LosingTrickCount.Count(hand),
            Shape = ShapeEvaluator.GetShape(hand),
            IsBalanced = ShapeEvaluator.IsBalanced(hand),
            RomanKeyCardCount = KeyCardCalculator.CalculateAll(hand),
            LongestAndStrongest = ShapeEvaluator.LongestAndStrongest(hand)
        };
    }
}