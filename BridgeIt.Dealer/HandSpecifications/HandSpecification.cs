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
    // Responses to 1-Suit Opening (Acol)
    // =============================================

    // Jacoby 2NT responder — 13+ HCP, 4+ card support in partner's major
    public static Func<Hand, bool> ResponseTo1Suit_Jacoby2NT(Suit partnerMajor) => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[partnerMajor] >= 4;

    // Simple major raise — 6-9 HCP, 4+ card support in partner's major
    public static Func<Hand, bool> ResponseTo1Suit_SimpleMajorRaise(Suit partnerMajor) => h =>
        HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9 &&
        ShapeEvaluator.GetShape(h)[partnerMajor] >= 4;

    // Limit major raise — 10-12 HCP, 4+ card support in partner's major
    public static Func<Hand, bool> ResponseTo1Suit_LimitMajorRaise(Suit partnerMajor) => h =>
        HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[partnerMajor] >= 4;

    // Game major raise — 13+ HCP, 4+ card support (if not playing Jacoby)
    public static Func<Hand, bool> ResponseTo1Suit_GameMajorRaise(Suit partnerMajor) => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[partnerMajor] >= 4;

    // New suit at 1 level — 6+ HCP, 4+ cards in a suit biddable at the 1 level
    public static Func<Hand, bool> ResponseTo1Suit_NewSuit1Level(Suit openingSuit) => h =>
    {
        var hcp = HighCardPoints.Count(h);
        if (hcp < 6) return false;
        var shape = ShapeEvaluator.GetShape(h);
        // Must have a 4+ card suit higher-ranking than opening suit (to bid at 1 level)
        return Enum.GetValues<Suit>()
            .Where(s => s > openingSuit && s != openingSuit)
            .Any(s => shape[s] >= 4);
    };

    // New suit at 2 level — 10+ HCP, 4+ cards in a suit that requires 2-level bid
    public static Func<Hand, bool> ResponseTo1Suit_NewSuit2Level(Suit openingSuit) => h =>
    {
        var hcp = HighCardPoints.Count(h);
        if (hcp < 10) return false;
        var shape = ShapeEvaluator.GetShape(h);
        // Has a 4+ card suit lower-ranking than opening suit (must bid at 2 level)
        return Enum.GetValues<Suit>()
            .Where(s => s < openingSuit)
            .Any(s => shape[s] >= 4);
    };

    // Simple minor raise — 6-9 HCP, 4+ card support in partner's minor, no 4-card major
    public static Func<Hand, bool> ResponseTo1Suit_SimpleMinorRaise(Suit partnerMinor) => h =>
        HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 9 &&
        ShapeEvaluator.GetShape(h)[partnerMinor] >= 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    // Limit minor raise — 10-12 HCP, 4+ support, no 4-card major
    public static Func<Hand, bool> ResponseTo1Suit_LimitMinorRaise(Suit partnerMinor) => h =>
        HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[partnerMinor] >= 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 4;

    // 1NT response — 6-9 HCP, no fit, no suit to show at 1 level
    public static Func<Hand, bool> ResponseTo1Suit_1NT(Suit openingSuit) => h =>
    {
        var hcp = HighCardPoints.Count(h);
        if (hcp < 6 || hcp > 9) return false;
        var shape = ShapeEvaluator.GetShape(h);
        // No 4+ card support for partner
        if (shape[openingSuit] >= 4) return false;
        // No 4+ card suit available at 1 level (higher than opening)
        var has1LevelSuit = Enum.GetValues<Suit>()
            .Where(s => s > openingSuit)
            .Any(s => shape[s] >= 4);
        return !has1LevelSuit;
    };

    // Pass response — less than 6 HCP, too weak to respond
    public static Func<Hand, bool> ResponseTo1Suit_Pass => h =>
        HighCardPoints.Count(h) < 6;

    // =============================================
    // Responses to 2NT Opening (Acol 20-22)
    // =============================================

    // Stayman over 2NT — 4+ HCP with exactly 4-card major (not 5+, which would transfer)
    public static Func<Hand, bool> ResponseTo2NT_Stayman => h =>
        HighCardPoints.Count(h) >= 4 &&
        (ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 || ShapeEvaluator.GetShape(h)[Suit.Spades] == 4) &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    // Transfer to hearts over 2NT — 5+ hearts
    public static Func<Hand, bool> ResponseTo2NT_TransferHearts => h =>
        ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5;

    // Transfer to spades over 2NT — 5+ spades, <5 hearts
    public static Func<Hand, bool> ResponseTo2NT_TransferSpades => h =>
        ShapeEvaluator.GetShape(h)[Suit.Spades] >= 5 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5;

    // =============================================
    // Opener rebid shapes (for testing full auction sequences)
    // =============================================

    // Opener with 5-card suit and a 4-card side suit (will rebid new suit)
    public static Func<Hand, bool> AcolOpenerWith5And4(Suit primary, Suit secondary) => h =>
        OneLevelUnbalancedOpening(h)
        && ShapeEvaluator.GetShape(h)[primary] >= 5
        && ShapeEvaluator.GetShape(h)[secondary] >= 4
        && ShapeEvaluator.LongestAndStrongest(h) == primary;

    // Opener with 6+ card suit (will rebid own suit)
    public static Func<Hand, bool> AcolOpenerWith6Card(Suit suit) => h =>
        OneLevelUnbalancedOpening(h)
        && ShapeEvaluator.GetShape(h)[suit] >= 6
        && ShapeEvaluator.LongestAndStrongest(h) == suit;

    // =============================================
    // After Stayman — Responder's rebid scenarios
    // =============================================

    // Game-forcing (13+), exactly 4 hearts (may or may not have 4 spades)
    public static Func<Hand, bool> AfterStayman_GameForcing_WithHearts => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    // Game-forcing (13+), exactly 4 spades, <4 hearts
    public static Func<Hand, bool> AfterStayman_GameForcing_WithSpades => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4;

    // Game-forcing (13+), both 4 hearts and 4 spades
    public static Func<Hand, bool> AfterStayman_GameForcing_BothMajors => h =>
        HighCardPoints.Count(h) >= 13 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] == 4;

    // Invitational (11-12), exactly 4 hearts (may or may not have 4 spades)
    public static Func<Hand, bool> AfterStayman_Invitational_WithHearts => h =>
        HighCardPoints.Count(h) >= 11 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] < 5;

    // Invitational (11-12), exactly 4 spades, <4 hearts
    public static Func<Hand, bool> AfterStayman_Invitational_WithSpades => h =>
        HighCardPoints.Count(h) >= 11 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] < 4;

    // Invitational (11-12), both 4 hearts and 4 spades
    public static Func<Hand, bool> AfterStayman_Invitational_BothMajors => h =>
        HighCardPoints.Count(h) >= 11 && HighCardPoints.Count(h) <= 12 &&
        ShapeEvaluator.GetShape(h)[Suit.Hearts] == 4 &&
        ShapeEvaluator.GetShape(h)[Suit.Spades] == 4;

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
