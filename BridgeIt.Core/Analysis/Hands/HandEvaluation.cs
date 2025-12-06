using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Primatives;
using YamlDotNet.Core.Tokens;

namespace BridgeIt.Core.Analysis.Hands;

public class HandEvaluation
{
    public int Hcp { get; init; }
    public int Losers { get; init; }
    public Dictionary<Suit,int> Shape { get; init; } = new();
    public bool IsBalanced { get; init; }
    public Dictionary<Suit,int> RKCB { get; init; } = new();
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
            RKCB = KeyCardCalculator.CalculateAll(hand)
        };
    }
}