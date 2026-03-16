using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dealer.HandSpecifications;

public static class HandSpecification
{
    public static Func<Hand, bool> BasicPuppetStaymanOpener =>
        h => HighCardPoints.Count(h) >= 20 && HighCardPoints.Count(h) <= 22 && ShapeEvaluator.IsBalanced(h);

    public static Func<Hand, bool> BasicPuppetStaymanResponder =>
        h => HighCardPoints.Count(h) >= 4 && ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 4 && 
             ShapeEvaluator.GetShape(h)[Suit.Spades] <=4;
    
    public static Func<Dictionary<Seat,Hand>, bool> HasSpadeOrHeartFit(Seat opener, Seat responder) =>
        h => ShapeEvaluator.GetShape(h[opener])[Suit.Spades] + ShapeEvaluator.GetShape(h[responder])[Suit.Spades] >= 8
        || ShapeEvaluator.GetShape(h[opener])[Suit.Hearts] + ShapeEvaluator.GetShape(h[responder])[Suit.Hearts] >= 8;
    
    //Building blocks
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
    
    
    
    //Basic Acol
    public static Func<Hand, bool> Acol1NtOpening => BalancedOpener(12, 14);
    public static Func<Hand, bool> Acol2NtOpening => BalancedOpener(20, 22);
    
    //TODO: Currently simplified to only balanced but could be other types for pass
    public static Func<Hand, bool> AcolOpeningPass => BalancedOpener(1, 11);

    private static Func<Hand, bool> OneLevelUnbalancedOpening =>
        h => HighCardPoints.Count(h) >= 12
             && HighCardPoints.Count(h) <= 19
             && !IsBalanced(h)
             && LosingTrickCount.Count(h) > 4;
    
    public static Func<Hand, bool> AcolMajor1LevelOpening(Suit suit) => h => OneLevelUnbalancedOpening(h)
                                                                             && ShapeEvaluator.LongestAndStrongest(h) == suit;
    
    public static Func<Hand, bool> AcolMinor1LevelOpening(Suit suit) => h => OneLevelUnbalancedOpening(h)
                                                                             && ShapeEvaluator.LongestAndStrongest(h) == suit 
                                                                             && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5
                                                                             && ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    public static Func<Hand, bool> AcolWeakAndLongOpening(Suit suit, int num = 6) => h =>
        ShapeEvaluator.GetShape(h)[suit] == num
        && ShapeEvaluator.LongestAndStrongest(h) == suit
        && HighCardPoints.Count(h) < 10
        && HighCardPoints.Count(h) >= 6;














}