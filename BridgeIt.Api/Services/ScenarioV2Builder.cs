using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;

namespace BridgeIt.Api.Services;

/// <summary>
/// Builds a ScenarioDeal from a (northRole, situation) pair.
/// North role selects what kind of hand North gets.
/// Situation adjusts South and opponent constraints to create the desired practice context.
/// </summary>
public static class ScenarioV2Builder
{
    private static readonly Func<Hand, bool> Pass = HandSpecification.PassingOpponent;

    public static ScenarioDeal Build(string northRole, string situation)
    {
        var north = GetNorthConstraint(northRole);
        var south = GetSouthConstraint(northRole, situation);
        var (east, west) = GetOpponentConstraints(situation);
        var name = $"{FormatRole(northRole)} — {FormatSituation(situation)}";
        return new ScenarioDeal(name, "Practice", north, east, south, west);
    }

    // ── North constraint ─────────────────────────────────────────────────────

    private static Func<Hand, bool> GetNorthConstraint(string role) => role switch
    {
        "any-opening" => h =>
            HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 19,

        "1nt-opener" => HandSpecification.Acol1NtOpening,

        "strong-2c" => h => HighCardPoints.Count(h) >= 20,

        "2nt-opener" => HandSpecification.Acol2NtOpening,

        "major-opener" => h =>
            (HandSpecification.AcolMajor1LevelOpening(Suit.Hearts)(h)
             || HandSpecification.AcolMajor1LevelOpening(Suit.Spades)(h)),

        "suit-unbalanced" => HandSpecification.OneLevelUnbalancedOpening,

        "bal-15-17" => HandSpecification.Acol1NtRebid,

        "bal-18-19" => HandSpecification.Acol2NtRebid,

        "responding" => h =>
            HighCardPoints.Count(h) >= 6 && HighCardPoints.Count(h) <= 12,

        _ => h => HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 19,
    };

    // ── South constraint (depends on both North's role and the situation) ────

    private static Func<Hand, bool> GetSouthConstraint(string northRole, string situation)
    {
        // When North is responding, South must be the opener
        if (northRole == "responding")
            return GetSouthAsOpener(situation);

        // Otherwise North opens and South responds
        return GetSouthAsResponder(northRole, situation);
    }

    private static Func<Hand, bool> GetSouthAsOpener(string situation) => situation switch
    {
        "nt-auction" => HandSpecification.Acol1NtOpening,

        "major-fit" => h =>
            (HandSpecification.AcolMajor1LevelOpening(Suit.Hearts)(h)
             || HandSpecification.AcolMajor1LevelOpening(Suit.Spades)(h)),

        "slam" => h => HighCardPoints.Count(h) >= 20,

        // Default: any opening hand for South
        _ => h => HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 19,
    };

    private static Func<Hand, bool> GetSouthAsResponder(string northRole, string situation)
    {
        return situation switch
        {
            "any" => DefaultResponder(),

            "nt-auction" => h =>
                ShapeEvaluator.IsBalanced(h)
                && HighCardPoints.Count(h) >= 6,

            "major-fit" => northRole switch
            {
                // North opened 1NT — South needs a 4+ card major for Stayman/transfer
                "1nt-opener" or "2nt-opener" => h =>
                    HighCardPoints.Count(h) >= 6
                    && (ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4
                        || ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4),

                // North opened a major — South needs 4+ support
                "major-opener" => h =>
                    HighCardPoints.Count(h) >= 6
                    && (ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4
                        || ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4),

                // Any opening — South just needs some values and a major
                _ => h =>
                    HighCardPoints.Count(h) >= 6
                    && (ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 4
                        || ShapeEvaluator.GetShape(h)[Suit.Spades] >= 4),
            },

            "slam" => northRole switch
            {
                "1nt-opener" => h => HighCardPoints.Count(h) >= 19,
                "2nt-opener" or "strong-2c" => h => HighCardPoints.Count(h) >= 10,
                _ => h => HighCardPoints.Count(h) >= 17,
            },

            "competitive" => DefaultResponder(),

            "misfit" => northRole switch
            {
                // Misfit: South is short in North's likely suits
                "major-opener" => h =>
                    HighCardPoints.Count(h) >= 6
                    && ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 2
                    && ShapeEvaluator.GetShape(h)[Suit.Spades] <= 2,

                "1nt-opener" or "2nt-opener" => h =>
                    HighCardPoints.Count(h) >= 6
                    && !ShapeEvaluator.IsBalanced(h)
                    && ShapeEvaluator.GetShape(h).Values.Max() >= 6,

                _ => h =>
                    HighCardPoints.Count(h) >= 6
                    && !ShapeEvaluator.IsBalanced(h),
            },

            _ => DefaultResponder(),
        };
    }

    private static Func<Hand, bool> DefaultResponder() => h =>
        HighCardPoints.Count(h) >= 6;

    // ── Opponent constraints ─────────────────────────────────────────────────

    private static (Func<Hand, bool>? East, Func<Hand, bool>? West) GetOpponentConstraints(
        string situation)
    {
        if (situation == "competitive")
        {
            // Opponents get opening-strength hands — at least one should be able to bid
            Func<Hand, bool> activeOpponent = h =>
                HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 16;
            return (activeOpponent, null); // East overcalls, West unconstrained
        }

        return (Pass, Pass);
    }

    // ── Display helpers ──────────────────────────────────────────────────────

    private static string FormatRole(string role) => role switch
    {
        "any-opening" => "Any opening",
        "1nt-opener" => "1NT opener",
        "strong-2c" => "Strong 2♣",
        "2nt-opener" => "2NT opener",
        "major-opener" => "Major opener",
        "suit-unbalanced" => "Suit opener (unbal)",
        "bal-15-17" => "1NT rebid (15\u201317)",
        "bal-18-19" => "2NT rebid (18\u201319)",
        "responding" => "Responding",
        _ => role,
    };

    private static string FormatSituation(string situation) => situation switch
    {
        "any" => "Any",
        "nt-auction" => "NT auction",
        "major-fit" => "Major fit",
        "slam" => "Slam potential",
        "competitive" => "Competitive",
        "misfit" => "Misfit",
        _ => situation,
    };
}
