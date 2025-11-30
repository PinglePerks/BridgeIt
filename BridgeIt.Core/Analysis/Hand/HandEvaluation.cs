using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hand;

public class HandEvaluation
{
    public int Hcp { get; init; }
    public int Losers { get; init; }
    public Dictionary<Suit,int> Shape { get; init; } = new();
    public bool IsBalanced { get; init; }
}

public static class HandEvaluator
{
    public static HandEvaluation Evaluate(Domain.Primatives.Hand hand)
    {
        return new HandEvaluation
        {
            Hcp = HighCardPoints.Count(hand),
            Losers = LosingTrickCount.Count(hand),
            Shape = ShapeEvaluator.GetShape(hand),
            IsBalanced = ShapeEvaluator.IsBalanced(hand)
        };
    }
}