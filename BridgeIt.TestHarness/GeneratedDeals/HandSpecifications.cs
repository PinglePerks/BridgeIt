using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.TestHarness.DealerIntegrationTests;

public static class HandSpecifications
{
    public static bool IsBalanced(Hand h) => ShapeEvaluator.IsBalanced(h);

    public static Func<Hand, bool> BalancedOpener(int minHcp, int maxHcp) =>
        h => IsBalanced(h) && HighCardPoints.Count(h) >= minHcp && HighCardPoints.Count(h) <= maxHcp;

    public static Func<Hand, bool> Open1NT => BalancedOpener(12, 14);

    public static Func<Hand, bool> Strong2NTOpener => BalancedOpener(20, 22);

    public static Func<Hand, bool> TransferToSpadesResponder =>
        h => ShapeEvaluator.GetShape(h)[Suit.Spades] >= 5 &&
             ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 4; // Using new dictionary-based method

    public static Func<Hand, bool> TransferToHeartsResponder =>
        h => ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5; // Using new dictionary-based method

    // "Stayman Hand" (11+ HCP, 4-card Major)
    public static Func<Hand, bool> StaymanResponder => h =>
        HighCardPoints.Count(h) >= 11 &&
        (ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 || ShapeEvaluator.GetShape(h)[Suit.Spades] == 4);

    public static Func<Hand, bool> PreemptHand(Suit suit) => h =>
        HighCardPoints.Count(h) <= 10 &&
        ShapeEvaluator.GetShape(h)[suit] >= 7;

    public static Func<Hand, bool> Spades2Response => h =>
        HighCardPoints.Count(h) == 11 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    public static Func<Hand, bool> NT2Response => h =>
        HighCardPoints.Count(h) == 12 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    public static Func<Hand, bool> WeakPass => h =>
        HighCardPoints.Count(h) < 11 &&
        ShapeEvaluator.IsBalanced(h);
    
    public static Func<Hand, bool> Hearts5Cards(int minHcp, int maxHcp) => h =>
        HighCardPoints.Count(h) >=  minHcp &&
        HighCardPoints.Count(h) <= maxHcp &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5;
    
    public static Func<Hand, bool> Hearts5CardsLosers(int minLosers, int maxLosers) => h =>
        LosingTrickCount.Count(h) >=  minLosers &&
        LosingTrickCount.Count(h) <= maxLosers &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5;
    
    public static Func<Hand, bool> Hearts5Clubs4(int minHcp, int maxHcp) => h =>
        HighCardPoints.Count(h) >=  minHcp &&
        HighCardPoints.Count(h) <= maxHcp &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] == 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Clubs] == 4;
    
    
    
    

}