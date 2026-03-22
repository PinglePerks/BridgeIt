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

    // =============================================
    // Building Blocks
    // =============================================

    public static bool IsBalanced(Hand h) => ShapeEvaluator.IsBalanced(h);

    public static Func<Hand, bool> BalancedOpener(int minHcp, int maxHcp) =>
        h => IsBalanced(h) && HighCardPoints.Count(h) >= minHcp && HighCardPoints.Count(h) <= maxHcp;

    public static Func<Hand, bool> Open1NT => BalancedOpener(12, 14);

    public static Func<Hand, bool> Strong2NTOpener => BalancedOpener(20, 22);

    public static Func<Hand, bool> TransferToSpadesResponder =>
        h => ShapeEvaluator.GetShape(h)[Suit.Spades] >= 5 &&
             ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 4;

    public static Func<Hand, bool> TransferToHeartsResponder =>
        h => ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5;

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

    // =============================================
    // Basic Acol Openings
    // =============================================

    public static Func<Hand, bool> Acol1NtOpening => BalancedOpener(12, 14);
    
    public static Func<Hand, bool> Acol1NtRebid => BalancedOpener(15, 17);
    public static Func<Hand, bool> Acol2NtRebid => BalancedOpener(18, 19);
    public static Func<Hand, bool> Acol2NtOpening => BalancedOpener(20, 22);

    // Any hand that should pass as dealer — sub-opening strength AND no weak preempt shape
    public static Func<Hand, bool> AcolOpeningPass => h =>
        HighCardPoints.Count(h) < 12 &&
        (HighCardPoints.Count(h) < 6 || ShapeEvaluator.GetShape(h).Values.All(v => v < 6));

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

    // =============================================
    // Responses to 1NT Opening (Acol 12-14)
    // =============================================

    // Weak hand, no 5+ major — should Pass
    public static Func<Hand, bool> ResponseTo1NT_WeakPass => h =>
        HighCardPoints.Count(h) < 11 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    // Weak takeout — 5+ hearts, too weak for Stayman/game try
    public static Func<Hand, bool> ResponseTo1NT_WeakTakeoutHearts => h =>
        HighCardPoints.Count(h) < 11 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5;

    // Weak takeout — 5+ spades (no 5+ hearts), too weak for game try
    public static Func<Hand, bool> ResponseTo1NT_WeakTakeoutSpades => h =>
        HighCardPoints.Count(h) < 11 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] >= 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5;

    // Invitational (11-12 HCP), no 4-card major — bids 2NT
    public static Func<Hand, bool> ResponseTo1NT_Invitational => h =>
        HighCardPoints.Count(h) >= 11 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    // Game-forcing (13+ HCP), no 4-card major — bids 3NT
    public static Func<Hand, bool> ResponseTo1NT_GameForcing => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    // Stayman — 11+ HCP with exactly 4-card major (not 5+, which would transfer)
    public static Func<Hand, bool> ResponseTo1NT_Stayman => h =>
        HighCardPoints.Count(h) >= 11 &&
        (ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 || ShapeEvaluator.GetShape(h)[Suit.Spades] == 4) &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    // =============================================
    // Opponent Specs (for uncontested test scenarios)
    // =============================================

    // Non-interfering opponent — will always pass (no opening strength, no preempt shape)
    public static Func<Hand, bool> PassingOpponent => h =>
        HighCardPoints.Count(h) < 12 &&
        (HighCardPoints.Count(h) < 6 || ShapeEvaluator.GetShape(h).Values.All(v => v < 6));

    // =============================================
    // Generic Hand Generator
    // =============================================

    public static Func<Hand, bool> Generator(
        int minHcpPoints,
        int maxHcpPoints,
        Dictionary<Suit, int> minShape,
        Dictionary<Suit, int> maxShape,
        Suit? targetSuit = null,
        bool longestAndStronger = true)
    {
        return h =>
        {
            // 1. Check High Card Points
            int hcp = HighCardPoints.Count(h);
            if (hcp < minHcpPoints || hcp > maxHcpPoints)
                return false;

            // 2. Evaluate the Hand's Shape
            var actualShape = ShapeEvaluator.GetShape(h);

            // 3. Check Minimum Shape Constraints
            if (minShape != null && !minShape.All(kv => actualShape.GetValueOrDefault(kv.Key, 0) >= kv.Value))
                return false;

            // 4. Check Maximum Shape Constraints
            if (maxShape != null && !maxShape.All(kv => actualShape.GetValueOrDefault(kv.Key, 0) <= kv.Value))
                return false;

            // 5. Check Longest and Strongest Suit Target
            if (longestAndStronger && targetSuit.HasValue)
            {
                if (ShapeEvaluator.LongestAndStrongest(h) != targetSuit.Value)
                    return false;
            }

            // If it survives all checks, it's a valid hand!
            return true;
        };
    }
}
