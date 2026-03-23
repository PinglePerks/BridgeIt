using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;

namespace BridgeIt.Api.Services;

public record ScenarioDeal(
    string DisplayName,
    string Category,
    Func<Hand, bool> North,
    Func<Hand, bool>? East = null,
    Func<Hand, bool>? South = null,
    Func<Hand, bool>? West = null);

public static class ScenarioRegistry
{
    // Passing opponent: < 12 HCP, no preempt shape
    private static readonly Func<Hand, bool> Pass = HandSpecification.PassingOpponent;

    public static readonly IReadOnlyDictionary<string, ScenarioDeal> All =
        new Dictionary<string, ScenarioDeal>
        {
            // ── Openings ──────────────────────────────────────────────────────
            ["open_1nt"] = new("Open 1NT (12–14 bal)", "Openings",
                HandSpecification.Acol1NtOpening, Pass, Pass, Pass),

            ["open_1h"] = new("Open 1♥", "Openings",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass, Pass, Pass),

            ["open_1s"] = new("Open 1♠", "Openings",
                HandSpecification.AcolMajor1LevelOpening(Suit.Spades), Pass, Pass, Pass),

            ["open_1d"] = new("Open 1♦", "Openings",
                HandSpecification.AcolMinor1LevelOpening(Suit.Diamonds), Pass, Pass, Pass),

            ["open_1c"] = new("Open 1♣", "Openings",
                HandSpecification.AcolMinor1LevelOpening(Suit.Clubs), Pass, Pass, Pass),

            ["open_2nt"] = new("Open 2NT (20–22 bal)", "Openings",
                HandSpecification.Acol2NtOpening, Pass, Pass, Pass),

            // ── Responses to 1NT ──────────────────────────────────────────────
            ["1nt_stayman"] = new("1NT → Stayman", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.ResponseTo1NT_Stayman, Pass),

            ["1nt_transfer_h"] = new("1NT → Transfer ♥", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.TransferToHeartsResponder, Pass),

            ["1nt_transfer_s"] = new("1NT → Transfer ♠", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.TransferToSpadesResponder, Pass),

            ["1nt_invite"] = new("1NT → Invite 2NT", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.ResponseTo1NT_Invitational, Pass),

            ["1nt_game_force"] = new("1NT → 3NT Game Force", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.ResponseTo1NT_GameForcing, Pass),

            ["1nt_weak_pass"] = new("1NT → Weak Pass", "Responses to 1NT",
                HandSpecification.Acol1NtOpening, Pass, HandSpecification.ResponseTo1NT_WeakPass, Pass),

            // ── Responses to 1-suit ───────────────────────────────────────────
            ["1h_simple_raise"] = new("1♥ → Simple Raise (6–9)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass,
                HandSpecification.ResponseTo1Suit_SimpleMajorRaise(Suit.Hearts), Pass),

            ["1s_simple_raise"] = new("1♠ → Simple Raise (6–9)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Spades), Pass,
                HandSpecification.ResponseTo1Suit_SimpleMajorRaise(Suit.Spades), Pass),

            ["1h_limit_raise"] = new("1♥ → Limit Raise (10–12)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass,
                HandSpecification.ResponseTo1Suit_LimitMajorRaise(Suit.Hearts), Pass),

            ["1s_limit_raise"] = new("1♠ → Limit Raise (10–12)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Spades), Pass,
                HandSpecification.ResponseTo1Suit_LimitMajorRaise(Suit.Spades), Pass),

            ["1h_jacoby_2nt"] = new("1♥ → Jacoby 2NT (13+)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass,
                HandSpecification.ResponseTo1Suit_Jacoby2NT(Suit.Hearts), Pass),

            ["1s_jacoby_2nt"] = new("1♠ → Jacoby 2NT (13+)", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Spades), Pass,
                HandSpecification.ResponseTo1Suit_Jacoby2NT(Suit.Spades), Pass),

            ["1h_new_suit_1"] = new("1♥ → New Suit 1-level", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass,
                HandSpecification.ResponseTo1Suit_NewSuit1Level(Suit.Hearts), Pass),

            ["1h_1nt_response"] = new("1♥ → 1NT Response", "Responses to 1-Suit",
                HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), Pass,
                HandSpecification.ResponseTo1Suit_1NT(Suit.Hearts), Pass),
        };
}
