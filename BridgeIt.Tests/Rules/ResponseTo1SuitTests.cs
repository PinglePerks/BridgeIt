using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

/// <summary>
/// Tests for all response rules when partner opens 1 of a suit (Acol).
/// These tests are written BEFORE the rule implementations — they define
/// the expected behaviour that the rules must satisfy.
///
/// Rule classes expected:
///   AcolJacoby2NTOver1Major   (priority ~60) — 13+ HCP, 4+ support, game forcing
///   AcolRaiseMajorOver1Suit   (priority ~50) — 4+ support, level by strength
///   AcolNewSuitOver1Suit      (priority ~40) — new suit, 1-level (6+) or 2-level (10+)
///   AcolRaiseMinorOver1Suit   (priority ~35) — 4+ minor support, no major to show
///   Acol1NTResponseTo1Suit    (priority ~30) — 6-9 HCP catch-all
/// </summary>
[TestFixture]
public class ResponseTo1SuitTests
{
    // =============================================================
    // HELPERS
    // =============================================================

    /// <summary>
    /// Creates a DecisionContext where North opens 1 of the given suit,
    /// East passes, and South is the responder.
    /// Partner's knowledge is set to the standard Acol 1-suit range (12-19, 4+ in suit).
    /// </summary>
    private static DecisionContext CreateResponseTo1SuitContext(
        Suit openingSuit, int hcp, Dictionary<Suit, int> shape,
        Suit? longestSuit = null)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, openingSuit)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = shape.Values.OrderByDescending(v => v).ToArray() switch
            {
                [4, 3, 3, 3] => true,
                [4, 4, 3, 2] => true,
                [5, 3, 3, 2] => true,
                _ => false
            },
            Losers = 7,
            LongestAndStrongest = longestSuit ?? shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key
        };

        var aucEval = AuctionEvaluator.Evaluate(history);

        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 19;
        tableKnowledge.Partner.MinShape[openingSuit] = 4;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a context that is NOT a response to 1-suit — e.g. partner opened 1NT.
    /// Used to verify rules reject wrong auction states.
    /// </summary>
    private static DecisionContext CreateWrongAuctionContext(int hcp = 10)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));

        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 4 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };

        var handEval = new HandEvaluation
        {
            Hcp = hcp, Shape = shape, IsBalanced = false, Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.South);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a context where it's NOT round 1 — responder has already had a turn.
    /// </summary>
    private static DecisionContext CreateRound2Context(Suit openingSuit, int hcp = 10)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, openingSuit)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(1, Suit.Spades))); // South's first response
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(2, openingSuit))); // Opener rebids
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        // South's turn again — BiddingRound = 2

        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 3 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 2 } };
        var handEval = new HandEvaluation
        {
            Hcp = hcp, Shape = shape, IsBalanced = false, Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.South);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 19;
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.South, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // Reusable shapes
    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    // =============================================================
    //  1. JACOBY 2NT OVER 1 MAJOR
    //     Rule: AcolJacoby2NTOver1Major
    //     Partner opens 1H/1S, responder has 4+ support and 13+ HCP
    //     Bids 2NT (conventional, game-forcing)
    // =============================================================

    #region Jacoby 2NT — CouldMakeBid

    [Test]
    public void Jacoby2NT_CouldMakeBid_TrueWith4Hearts13Hcp_Over1H()
    {
        // 13 HCP, 4 hearts — classic Jacoby 2NT hand over 1H
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 13, Shape(3, 4, 3, 3));
        // Rule should accept: 13+ HCP, 4+ support in partner's major
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_TrueWith5Spades16Hcp_Over1S()
    {
        // 16 HCP, 5 spades — strong game-forcing raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 16, Shape(5, 3, 3, 2));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseWith3Hearts13Hcp_InsufficientSupport()
    {
        // 13 HCP but only 3 hearts — not enough support
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 13, Shape(4, 3, 3, 3));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseWith4Hearts12Hcp_InsufficientPoints()
    {
        // 12 HCP with 4 hearts — not enough for game force
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 12, Shape(3, 4, 3, 3));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseOver1Diamond_NotAMajor()
    {
        // Partner opened 1D — Jacoby 2NT only applies to majors
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 14, Shape(3, 3, 4, 3));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseOver1Club_NotAMajor()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 14, Shape(3, 3, 3, 4));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseWhenPartnerOpened1NT()
    {
        var ctx = CreateWrongAuctionContext(14);
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldMakeBid_FalseWhenNotRound1()
    {
        var ctx = CreateRound2Context(Suit.Hearts, 14);
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Jacoby 2NT — Apply

    [Test]
    public void Jacoby2NT_Apply_Returns2NT_Over1H()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 14, Shape(3, 4, 3, 3));
        Assert.That(GetJacoby2NTRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Jacoby2NT_Apply_Returns2NT_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 15, Shape(4, 3, 4, 2));
        Assert.That(GetJacoby2NTRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    #endregion

    #region Jacoby 2NT — CouldExplainBid (backward inference)

    [Test]
    public void Jacoby2NT_CouldExplainBid_TrueFor2NT_Over1Major()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3)); // hand doesn't matter for backward
        Assert.That(GetJacoby2NTRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void Jacoby2NT_CouldExplainBid_FalseFor3NT()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetJacoby2NTRule().CouldExplainBid(Bid.NoTrumpsBid(3), ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldExplainBid_FalseForSuitBid()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetJacoby2NTRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    [Test]
    public void Jacoby2NT_CouldExplainBid_FalseOver1Minor()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 0, Shape(3, 3, 4, 3));
        Assert.That(GetJacoby2NTRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    #endregion

    #region Jacoby 2NT — GetConstraintForBid

    [Test]
    public void Jacoby2NT_GetConstraint_2NT_HasHcp13PlusAndSuitSupport()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 14, Shape(3, 4, 3, 3));
        var info = GetJacoby2NTRule().GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        // Should contain HCP constraint (13+)
        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(13));

        // Should contain suit length constraint for partner's major (4+)
        var suitConstraint = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint, Is.Not.Null);
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
        Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));

        // Game-forcing state
        Assert.That(info.PartnershipBiddingState,
            Is.EqualTo(PartnershipBiddingState.ConstructiveSearch)
                .Or.EqualTo(PartnershipBiddingState.FitEstablished));
    }

    #endregion

    // =============================================================
    //  2. RAISE MAJOR OVER 1 SUIT
    //     Rule: AcolRaiseMajorOver1Suit
    //     Partner opens 1H/1S, responder has 4+ support
    //     Simple raise (6-9) → 2H/2S
    //     Limit raise (10-12) → 3H/3S
    //     Game raise (13+, no slam) → 4H/4S
    //     Note: hands with 13+ and 4+ support should be caught by
    //     Jacoby 2NT first (higher priority), so the game raise here
    //     is mainly for when Jacoby isn't being played, or edge cases.
    // =============================================================

    #region Raise Major — CouldMakeBid

    [Test]
    public void RaiseMajor_CouldMakeBid_TrueWith4Hearts8Hcp()
    {
        // Simple raise territory: 4 hearts, 8 HCP
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 4, 4, 2));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_TrueWith5Spades6Hcp()
    {
        // Minimum simple raise: 5 spades, 6 HCP
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 6, Shape(5, 3, 3, 2));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_TrueWith4Hearts11Hcp_LimitRaise()
    {
        // Limit raise territory: 4 hearts, 11 HCP
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 11, Shape(3, 4, 3, 3));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_FalseWith3Hearts8Hcp_InsufficientSupport()
    {
        // Only 3 hearts — not enough to raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 3, 4, 2));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_FalseWith4Hearts5Hcp_TooWeak()
    {
        // 5 HCP — below minimum for a raise (need 6)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 5, Shape(3, 4, 4, 2));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_FalseOverMinorOpening()
    {
        // Partner opened 1D — can't raise a minor with this rule
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_FalseWhenWrongAuction()
    {
        var ctx = CreateWrongAuctionContext(8);
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldMakeBid_FalseWhenNotRound1()
    {
        var ctx = CreateRound2Context(Suit.Hearts, 8);
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Raise Major — Apply (level by strength)

    [Test]
    [TestCase(6, "2H", Description = "Minimum simple raise")]
    [TestCase(7, "2H", Description = "Simple raise")]
    [TestCase(9, "2H", Description = "Maximum simple raise")]
    public void RaiseMajor_Apply_SimpleRaise_Over1H(int hcp, string expectedBid)
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, hcp, Shape(3, 4, 4, 2));
        Assert.That(GetRaiseMajorRule().Apply(ctx)!.ToString(), Is.EqualTo(expectedBid));
    }

    [Test]
    [TestCase(10, "3H", Description = "Minimum limit raise")]
    [TestCase(11, "3H", Description = "Limit raise")]
    [TestCase(12, "3H", Description = "Maximum limit raise")]
    public void RaiseMajor_Apply_LimitRaise_Over1H(int hcp, string expectedBid)
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, hcp, Shape(3, 4, 4, 2));
        Assert.That(GetRaiseMajorRule().Apply(ctx)!.ToString(), Is.EqualTo(expectedBid));
    }

    [Test]
    public void RaiseMajor_Apply_SimpleRaise_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 7, Shape(4, 3, 3, 3));
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void RaiseMajor_Apply_LimitRaise_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 11, Shape(4, 3, 3, 3));
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void RaiseMajor_Apply_GameRaise_Over1H_WhenJacobyNotPlayed()
    {
        // 13 HCP, 4 hearts — if Jacoby 2NT didn't fire (not playing it),
        // this rule should bid 4H directly
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 13, Shape(3, 4, 4, 2));
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Hearts)));
    }

    [Test]
    public void RaiseMajor_Apply_GameRaise_Over1S_With5CardSupport()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 14, Shape(5, 2, 4, 2));
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Spades)));
    }

    #endregion

    #region Raise Major — CouldExplainBid

    [Test]
    public void RaiseMajor_CouldExplainBid_TrueFor2H_Over1H()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_TrueFor3H_Over1H()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_TrueFor4H_Over1H()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(4, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_TrueFor2S_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.True);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_FalseForDifferentSuit()
    {
        // Over 1H, bidding 2S is a new suit, not a raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_FalseForNTBid()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void RaiseMajor_CouldExplainBid_FalseFor5H_TooHigh()
    {
        // 5H over a 1H opening isn't a standard raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMajorRule().CouldExplainBid(Bid.SuitBid(5, Suit.Hearts), ctx), Is.False);
    }

    #endregion

    #region Raise Major — GetConstraintForBid

    [Test]
    public void RaiseMajor_GetConstraint_2H_SimpleRaise()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 4, 4, 2));
        var info = GetRaiseMajorRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(6));
        Assert.That(hcp.Max, Is.EqualTo(9));

        var suit = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suit, Is.Not.Null);
        Assert.That(suit!.Suit, Is.EqualTo(Suit.Hearts));
        Assert.That(suit.MinLen, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void RaiseMajor_GetConstraint_3S_LimitRaise()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 11, Shape(4, 3, 3, 3));
        var info = GetRaiseMajorRule().GetConstraintForBid(Bid.SuitBid(3, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp!.Min, Is.EqualTo(10));
        Assert.That(hcp.Max, Is.EqualTo(12));
    }

    [Test]
    public void RaiseMajor_GetConstraint_4H_GameRaise()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 14, Shape(3, 4, 4, 2));
        var info = GetRaiseMajorRule().GetConstraintForBid(Bid.SuitBid(4, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp!.Min, Is.EqualTo(13));
    }

    #endregion

    // =============================================================
    //  3. NEW SUIT RESPONSE OVER 1 SUIT
    //     Rule: AcolNewSuitOver1Suit
    //     Partner opens 1 of a suit, responder bids a different suit
    //     At 1 level: 6+ HCP, 4+ cards
    //     At 2 level: 10+ HCP, 4+ cards
    // =============================================================

    #region New Suit — CouldMakeBid

    [Test]
    public void NewSuit_CouldMakeBid_TrueWith4Spades6Hcp_Over1H_AtOneLevel()
    {
        // 1H opening, responder has 4 spades, 6 HCP — can bid 1S
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 6, Shape(4, 2, 4, 3));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldMakeBid_TrueWith4Hearts6Hcp_Over1D_AtOneLevel()
    {
        // 1D opening, responder has 4 hearts — can bid 1H
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 6, Shape(3, 4, 2, 4));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldMakeBid_TrueWith4Hearts6Hcp_Over1C_AtOneLevel()
    {
        // 1C opening, responder has 4 hearts — can bid 1H
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 6, Shape(3, 4, 4, 2));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldMakeBid_TrueWith5Clubs10Hcp_Over1S_AtTwoLevel()
    {
        // 1S opening, responder has 5 clubs, 10 HCP — can bid 2C (new suit at 2 level)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(2, 3, 3, 5));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldMakeBid_TrueWith4Diamonds10Hcp_Over1H_AtTwoLevel()
    {
        // 1H opening, responder has 4 diamonds, 10 HCP — can bid 2D
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 10, Shape(3, 2, 4, 4), Suit.Diamonds);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldMakeBid_FalseWith4Clubs8Hcp_Over1S_TooWeakForTwoLevel()
    {
        // 1S opening, only suit to show is clubs (2-level), but only 8 HCP — need 10
        // No 4+ card suit available at the 1 level
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 8, Shape(3, 3, 3, 4), Suit.Clubs);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldMakeBid_FalseWith5Hcp_TooWeak()
    {
        // 5 HCP — below minimum even for 1-level response
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 5, Shape(4, 2, 4, 3));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldMakeBid_FalseWithNoNewSuitToShow()
    {
        // 1S opening, responder has no 4+ card suit other than spades
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(4, 3, 3, 3));
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldMakeBid_FalseWhenWrongAuction()
    {
        var ctx = CreateWrongAuctionContext(10);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldMakeBid_FalseWhenNotRound1()
    {
        var ctx = CreateRound2Context(Suit.Hearts, 10);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region New Suit — Apply (suit selection logic)

    [Test]
    public void NewSuit_Apply_BidsSpadesAt1Level_Over1H()
    {
        // Partner opens 1H, we have 4 spades — bid 1S (cheapest available at 1 level)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 2, 4, 3), Suit.Spades);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void NewSuit_Apply_BidsHeartsAt1Level_Over1D()
    {
        // Partner opens 1D, we have 4 hearts and 4 spades — bid 1H (cheapest)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(4, 4, 2, 3));
        var bid = GetNewSuitRule().Apply(ctx);
        // With two 4-card majors, should bid the cheapest (1H) to let partner
        // raise hearts or bid 1S
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void NewSuit_Apply_BidsSpadesAt1Level_Over1C()
    {
        // Partner opens 1C, we have 5 spades — bid 1S
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 8, Shape(5, 3, 3, 2), Suit.Spades);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void NewSuit_Apply_BidsHeartsAt1Level_Over1C_WithBothMajors()
    {
        // Partner opens 1C, we have 4H + 4S — bid 1H (cheaper)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 8, Shape(4, 4, 3, 2));
        var bid = GetNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void NewSuit_Apply_BidsClubsAt2Level_Over1S()
    {
        // Partner opens 1S, we have 5 clubs, 10 HCP — 2C
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(2, 3, 3, 5), Suit.Clubs);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Clubs)));
    }

    [Test]
    public void NewSuit_Apply_BidsDiamondsAt2Level_Over1H()
    {
        // Partner opens 1H, we have 4D, 10 HCP, only 3 spades — bid 2D
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 10, Shape(3, 2, 5, 3), Suit.Diamonds);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void NewSuit_Apply_Prefers1LevelMajorOverTwoLevelMinor()
    {
        // Partner opens 1D, we have 4H and 4C — should bid 1H (at 1 level) not 2C
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 4, 2, 4));
        var bid = GetNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void NewSuit_Apply_LongestSuitFirst_5SpadesOver5Clubs_Over1D()
    {
        // Partner opens 1D, we have 5S and 5C, 10 HCP
        // With two 5+ card suits, bid the higher-ranking first
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 10, Shape(5, 1, 2, 5), Suit.Spades);
        var bid = GetNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void NewSuit_Apply_LongestSuitFirst_5Hearts5Clubs_Over1D()
    {
        // Partner opens 1D, we have 5H and 5C, 10 HCP
        // With two 5-card suits, bid the higher-ranking first
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 10, Shape(1, 5, 2, 5), Suit.Hearts);
        var bid = GetNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void NewSuit_Apply_6CardSuit_BidsIt()
    {
        // Partner opens 1H, we have 6 diamonds, 10 HCP — bid 2D
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 10, Shape(2, 2, 6, 3), Suit.Diamonds);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    #endregion

    #region New Suit — CouldExplainBid

    [Test]
    public void NewSuit_CouldExplainBid_TrueFor1S_Over1H()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(1, Suit.Spades), ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldExplainBid_TrueFor1H_Over1D()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 0, Shape(3, 3, 3, 4));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(1, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldExplainBid_TrueFor2C_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 0, Shape(3, 3, 3, 4));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldExplainBid_TrueFor2D_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void NewSuit_CouldExplainBid_FalseForSameSuitAsOpening()
    {
        // 2H over 1H is a raise, not a new suit
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldExplainBid_FalseForNTBid()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldExplainBid_FalseForPass()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.Pass(), ctx), Is.False);
    }

    [Test]
    public void NewSuit_CouldExplainBid_FalseFor1D_Over1H_NotCheaper()
    {
        // 1D is below 1H — you can't bid lower than partner's opening
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(GetNewSuitRule().CouldExplainBid(Bid.SuitBid(1, Suit.Diamonds), ctx), Is.False);
    }

    #endregion

    #region New Suit — GetConstraintForBid

    [Test]
    public void NewSuit_GetConstraint_1S_Over1H_Hcp6Plus()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 2, 4, 3));
        var info = GetNewSuitRule().GetConstraintForBid(Bid.SuitBid(1, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(6));

        var suit = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suit, Is.Not.Null);
        Assert.That(suit!.Suit, Is.EqualTo(Suit.Spades));
        Assert.That(suit.MinLen, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void NewSuit_GetConstraint_2C_Over1S_Hcp10Plus()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(2, 3, 3, 5));
        var info = GetNewSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Clubs), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp!.Min, Is.EqualTo(10));

        var suit = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suit!.Suit, Is.EqualTo(Suit.Clubs));
        Assert.That(suit.MinLen, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void NewSuit_GetConstraint_SetsConstructiveSearch()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 2, 4, 3));
        var info = GetNewSuitRule().GetConstraintForBid(Bid.SuitBid(1, Suit.Spades), ctx);
        Assert.That(info!.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.ConstructiveSearch));
    }

    #endregion

    // =============================================================
    //  4. RAISE MINOR OVER 1 SUIT
    //     Rule: AcolRaiseMinorOver1Suit
    //     Partner opens 1C/1D, responder has 4+ support, no major to show
    //     Simple raise (6-9) → 2C/2D
    //     Limit raise (10-12) → 3C/3D
    // =============================================================

    #region Raise Minor — CouldMakeBid

    [Test]
    public void RaiseMinor_CouldMakeBid_TrueWith4Diamonds8Hcp_Over1D()
    {
        // 4 diamonds, 8 HCP, no 4-card major — raise 1D to 2D
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_TrueWith5Clubs7Hcp_Over1C()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 7, Shape(3, 3, 2, 5));
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_FalseWith3Diamonds_InsufficientSupport()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 3, 3, 4));
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_FalseOverMajor()
    {
        // Partner opened 1H — this is a major raise rule's territory
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 4, 3, 3));
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_FalseWith5Hcp_TooWeak()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 5, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_FalseWhenHave4CardMajorAt1Level()
    {
        // 1D opening, 4 hearts available to show at 1 level — should bid 1H not raise
        // This rule should not fire when a new suit at 1 level is possible
        // (handled by the rule itself or by priority ordering)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 4, 4, 2));
        // Rule may return true (it's the priority system that resolves this)
        // or the rule itself may check for no available major — implementation choice
        // Testing that the PRIORITY system works: NewSuit(40) > RaiseMinor(35)
        // Both could fire, but NewSuit fires first.
        // This test documents the preferred behaviour if the rule itself rejects:
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseMinor_CouldMakeBid_FalseWhenNotRound1()
    {
        var ctx = CreateRound2Context(Suit.Diamonds, 8);
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Raise Minor — Apply

    [Test]
    [TestCase(6, "2D", Description = "Minimum simple raise")]
    [TestCase(9, "2D", Description = "Maximum simple raise")]
    public void RaiseMinor_Apply_SimpleRaise_Over1D(int hcp, string expectedBid)
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, hcp, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().Apply(ctx)!.ToString(), Is.EqualTo(expectedBid));
    }

    [Test]
    [TestCase(10, "3D", Description = "Minimum limit raise")]
    [TestCase(12, "3D", Description = "Maximum limit raise")]
    public void RaiseMinor_Apply_LimitRaise_Over1D(int hcp, string expectedBid)
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, hcp, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().Apply(ctx)!.ToString(), Is.EqualTo(expectedBid));
    }

    [Test]
    public void RaiseMinor_Apply_SimpleRaise_Over1C()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 7, Shape(3, 3, 2, 5));
        Assert.That(GetRaiseMinorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Clubs)));
    }

    #endregion

    #region Raise Minor — CouldExplainBid

    [Test]
    public void RaiseMinor_CouldExplainBid_TrueFor2D_Over1D()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void RaiseMinor_CouldExplainBid_TrueFor3C_Over1C()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 0, Shape(3, 3, 3, 4));
        Assert.That(GetRaiseMinorRule().CouldExplainBid(Bid.SuitBid(3, Suit.Clubs), ctx), Is.True);
    }

    [Test]
    public void RaiseMinor_CouldExplainBid_FalseForDifferentSuit()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 0, Shape(3, 3, 4, 3));
        Assert.That(GetRaiseMinorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.False);
    }

    [Test]
    public void RaiseMinor_CouldExplainBid_FalseOverMajor()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 4, 3, 3));
        Assert.That(GetRaiseMinorRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    #endregion

    #region Raise Minor — GetConstraintForBid

    [Test]
    public void RaiseMinor_GetConstraint_2D_SimpleRaise()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 3, 4, 3));
        var info = GetRaiseMinorRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Diamonds), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        var hcp = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcp!.Min, Is.EqualTo(6));
        Assert.That(hcp.Max, Is.EqualTo(9));

        var suit = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suit!.Suit, Is.EqualTo(Suit.Diamonds));
        Assert.That(suit.MinLen, Is.GreaterThanOrEqualTo(4));
    }

    #endregion

    // =============================================================
    //  5. 1NT RESPONSE TO 1 SUIT
    //     Rule: Acol1NTResponseTo1Suit
    //     6-9 HCP, no 4+ card support, no suit to show at 1 level
    //     Catch-all "I have something but nothing specific"
    // =============================================================

    #region 1NT Response — CouldMakeBid

    [Test]
    public void NT1Response_CouldMakeBid_TrueWith8Hcp_NoFit_NoSuit_Over1H()
    {
        // 1H opening, 8 HCP, no fit (3 hearts), no suit at 1 level (3 spades)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NT1Response_CouldMakeBid_TrueWith6Hcp_Minimum()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 6, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NT1Response_CouldMakeBid_TrueWith9Hcp_Maximum()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 9, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NT1Response_CouldMakeBid_FalseWith5Hcp_TooWeak()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 5, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NT1Response_CouldMakeBid_FalseWith10Hcp_TooStrong()
    {
        // 10 HCP — too strong for 1NT, should be making a limit raise or new suit
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 10, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NT1Response_CouldMakeBid_FalseWhenWrongAuction()
    {
        var ctx = CreateWrongAuctionContext(8);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NT1Response_CouldMakeBid_FalseWhenNotRound1()
    {
        var ctx = CreateRound2Context(Suit.Hearts, 8);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region 1NT Response — Apply

    [Test]
    public void NT1Response_Apply_Returns1NT()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void NT1Response_Apply_Returns1NT_Over1S()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 7, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void NT1Response_Apply_Returns1NT_Over1C()
    {
        // Even over a minor — 1NT shows 6-9 with nothing specific
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 8, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    #endregion

    #region 1NT Response — CouldExplainBid

    [Test]
    public void NT1Response_CouldExplainBid_TrueFor1NT()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.True);
    }

    [Test]
    public void NT1Response_CouldExplainBid_FalseFor2NT()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void NT1Response_CouldExplainBid_FalseForSuitBid()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldExplainBid(Bid.SuitBid(1, Suit.Spades), ctx), Is.False);
    }

    [Test]
    public void NT1Response_CouldExplainBid_FalseForPass()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 0, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldExplainBid(Bid.Pass(), ctx), Is.False);
    }

    #endregion

    #region 1NT Response — GetConstraintForBid

    [Test]
    public void NT1Response_GetConstraint_1NT_Hcp6to9()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 3, 4, 3));
        var info = Get1NTResponseRule().GetConstraintForBid(Bid.NoTrumpsBid(1), ctx);

        Assert.That(info, Is.Not.Null);

        // Should have HCP constraint (6-9) somewhere in the constraint tree
        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(6));
        Assert.That(hcp.Max, Is.EqualTo(9));
    }

    [Test]
    public void NT1Response_GetConstraint_SetsSignOff()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 3, 4, 3));
        var info = Get1NTResponseRule().GetConstraintForBid(Bid.NoTrumpsBid(1), ctx);
        Assert.That(info!.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.ConstructiveSearch));
    }

    #endregion

    // =============================================================
    //  6. PRIORITY / INTEGRATION SCENARIOS
    //     Verify that with all rules loaded, the right one fires.
    //     These tests instantiate ALL response-to-1-suit rules,
    //     sort by priority, and check which rule's CouldMakeBid fires first.
    // =============================================================

    #region Priority / Integration

    [Test]
    public void Priority_Jacoby2NT_FiresBeforeMajorRaise_For13HcpWith4Hearts()
    {
        // 13 HCP, 4 hearts over 1H — both Jacoby and RaiseMajor could fire
        // Jacoby should have higher priority
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 13, Shape(3, 4, 3, 3));

        var jacoby = GetJacoby2NTRule();
        var raise = GetRaiseMajorRule();

        Assert.That(jacoby.Priority, Is.GreaterThan(raise.Priority),
            "Jacoby 2NT should have higher priority than major raise");
        Assert.That(jacoby.CouldMakeBid(ctx), Is.True);
        Assert.That(raise.CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void Priority_NewSuitAt1Level_FiresBeforeMinorRaise()
    {
        // 1D opening, responder has 4H and 4D — both NewSuit and RaiseMinor could fire
        // NewSuit should fire first
        var ctx = CreateResponseTo1SuitContext(
            Suit.Diamonds, 8, Shape(3, 4, 4, 2));

        var newSuit = GetNewSuitRule();
        var raiseMinor = GetRaiseMinorRule();

        Assert.That(newSuit.Priority, Is.GreaterThan(raiseMinor.Priority),
            "New suit should have higher priority than minor raise");
    }

    [Test]
    public void Priority_MajorRaise_FiresBeforeNewSuit()
    {
        // 1H opening, 4 hearts, 8 HCP — raise should fire before new suit
        var raise = GetRaiseMajorRule();
        var newSuit = GetNewSuitRule();

        Assert.That(raise.Priority, Is.GreaterThan(newSuit.Priority),
            "Major raise should have higher priority than new suit");
    }

    [Test]
    public void Priority_1NTResponse_IsLowestPriority()
    {
        var nt = Get1NTResponseRule();
        var newSuit = GetNewSuitRule();
        var raise = GetRaiseMajorRule();
        var raiseMinor = GetRaiseMinorRule();

        Assert.That(nt.Priority, Is.LessThan(newSuit.Priority));
        Assert.That(nt.Priority, Is.LessThan(raise.Priority));
        Assert.That(nt.Priority, Is.LessThan(raiseMinor.Priority));
    }

    #endregion

    // =============================================================
    //  7. PASS SCENARIOS
    //     With <6 HCP, no rule should fire — responder passes.
    // =============================================================

    #region Pass Scenarios

    [Test]
    public void Pass_AllRulesReject_With4Hcp()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 4, Shape(3, 4, 3, 3));

        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Pass_AllRulesReject_With0Hcp()
    {
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 0, Shape(5, 3, 3, 2));

        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetRaiseMinorRule().CouldMakeBid(ctx), Is.False);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    // =============================================================
    //  8. EDGE CASES & BOUNDARY CONDITIONS
    // =============================================================

    #region Edge Cases

    [Test]
    public void EdgeCase_ExactlyOneBoundary_6Hcp_Over1H_With4Spades()
    {
        // 6 HCP is the minimum for both new suit at 1 level and simple raise
        // With 4 spades and 2 hearts, should bid new suit (1S) not raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 6, Shape(4, 2, 4, 3));

        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False, "No 4-card support");
    }

    [Test]
    public void EdgeCase_ExactlyOneBoundary_6Hcp_Over1S_WithNoSuit()
    {
        // 6 HCP, 3 in every non-spade suit — nothing to bid but 1NT
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 6, Shape(3, 3, 4, 3));
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void EdgeCase_9Hcp_BoundaryBetween1NTAndNewSuit()
    {
        // 9 HCP, 4 diamonds, over 1S — have a suit but only 9 HCP
        // 2D requires 10 HCP, so should bid 1NT instead
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 9, Shape(3, 3, 4, 3), Suit.Diamonds);

        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False,
            "9 HCP insufficient for 2-level new suit");
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void EdgeCase_10Hcp_JustEnoughForTwoLevel()
    {
        // 10 HCP, 4 clubs over 1S — just enough for 2C
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(3, 3, 3, 4), Suit.Clubs);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void EdgeCase_13Hcp_JacobyVsGameRaise()
    {
        // 13 HCP, 4 spades over 1S — Jacoby should take this, not a game raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 13, Shape(4, 3, 3, 3));

        // Both should be able to fire, but Jacoby has higher priority
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void EdgeCase_PassByOpponent_DoesntAffectResponse()
    {
        // Normal uncontested auction: North opens 1H, East passes, South responds
        // This should work exactly as normal
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 3, 3, 3));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False, "Only 3 hearts");
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True, "Can bid 1S with 4 spades");
    }

    [Test]
    public void EdgeCase_AllFourSuits3Cards_Over1H()
    {
        // 3-3-4-3 shape, 8 HCP, over 1H
        // No fit, no 4-card suit at 1 level (only 3 spades), diamonds need 2-level
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 3, 4, 3));

        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False, "Only 3 hearts");
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True, "1NT catch-all");
    }

    [Test]
    public void EdgeCase_Responder_With4OfBothMajors_Over1C()
    {
        // 1C opening, 4H and 4S, 8 HCP — should bid cheapest (1H)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Clubs, 8, Shape(4, 4, 3, 2));
        var bid = GetNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)),
            "With two 4-card majors, bid the cheaper one first");
    }

    [Test]
    public void EdgeCase_Responder_5HeartsFit_Plus4Spades_Over1H()
    {
        // 1H opening, 5 hearts (fit!) + 4 spades, 8 HCP
        // Should raise hearts, not bid 1S — fit takes priority
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(4, 5, 2, 2));

        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True,
            "Has 5-card heart support");
        Assert.That(GetRaiseMajorRule().Priority, Is.GreaterThan(GetNewSuitRule().Priority),
            "Raise should fire before new suit");
    }

    #endregion

    // =============================================================
    //  HARD-CODED HAND SCENARIOS
    //  Real-world hands to validate end-to-end behaviour
    // =============================================================

    #region Hard-coded Hands

    [Test]
    public void Hand_SAKQ_H72_DT984_C653_Over1H_Bids1S()
    {
        // 11 HCP (S:AKQ), 4 spades, 2 hearts — bid 1S (new suit at 1 level)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 11, Shape(4, 2, 4, 3), Suit.Spades);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void Hand_S73_HKJT5_DQ84_CJ962_Over1H_Raises2H()
    {
        // 8 HCP, 4 hearts — simple raise to 2H
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(2, 4, 3, 4));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void Hand_SJ4_H83_DK9752_CT64_Over1S_Bids1NT()
    {
        // 5 diamonds, 3 HCP — too weak to bid. Wait, let's make it 7 HCP
        // S:J4 (1), D:K9752 (3+), more honours...
        // 7 HCP, no fit with spades (2), diamonds at 2-level needs 10 — bid 1NT
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 7, Shape(2, 3, 5, 3), Suit.Diamonds);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.True);
        Assert.That(Get1NTResponseRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void Hand_SKQ94_HAJ73_D52_C864_Over1H_Jacoby2NT()
    {
        // 13 HCP, 4 hearts — Jacoby 2NT
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 13, Shape(4, 4, 2, 3));
        Assert.That(GetJacoby2NTRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetJacoby2NTRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void Hand_S862_HKQ74_DQJ3_C952_Over1H_Raises2H()
    {
        // 8 HCP, 4 hearts — simple raise
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 8, Shape(3, 4, 3, 3));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void Hand_SAK3_HKQJ5_DT72_C863_Over1H_LimitRaise3H()
    {
        // 12 HCP, 4 hearts — limit raise to 3H
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 12, Shape(3, 4, 3, 3));
        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetRaiseMajorRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void Hand_S943_H72_DT84_CJ9653_Over1H_Passes()
    {
        // 2 HCP, too weak to respond at all
        var ctx = CreateResponseTo1SuitContext(
            Suit.Hearts, 2, Shape(3, 2, 3, 5));

        Assert.That(GetRaiseMajorRule().CouldMakeBid(ctx), Is.False);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.False);
        Assert.That(Get1NTResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void Hand_SQ8_HAJT84_D72_CKJ53_Over1S_10Hcp_Bids2H_NewSuitAt2Level()
    {
        // 10 HCP, 5 hearts over 1S — bid 2H (new suit at 2 level, need 10+)
        var ctx = CreateResponseTo1SuitContext(
            Suit.Spades, 10, Shape(2, 5, 2, 4), Suit.Hearts);
        Assert.That(GetNewSuitRule().CouldMakeBid(ctx), Is.True);
        Assert.That(GetNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    #endregion

    // =============================================================
    // RULE FACTORY METHODS
    // These will need updating once you create the actual rule classes.
    // For now they use placeholder names matching the expected class names.
    // =============================================================

    // TODO: Replace these with actual rule instantiation once implemented
    // e.g.: private static BiddingRuleBase GetJacoby2NTRule() => new AcolJacoby2NTOver1Major();

    private static BiddingRuleBase GetJacoby2NTRule()
    {
        var type = Type.GetType("BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit.AcolJacoby2NTOver1Major, BridgeIt.Core");
        Assert.That(type, Is.Not.Null,
            "Rule class AcolJacoby2NTOver1Major not found. Create it at BiddingEngine/Rules/Responder/ResponsesTo1Suit/AcolJacoby2NTOver1Major.cs");
        return (BiddingRuleBase)Activator.CreateInstance(type!)!;
    }

    private static BiddingRuleBase GetRaiseMajorRule()
    {
        var type = Type.GetType("BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit.AcolRaiseMajorOver1Suit, BridgeIt.Core");
        Assert.That(type, Is.Not.Null,
            "Rule class AcolRaiseMajorOver1Suit not found. Create it at BiddingEngine/Rules/Responder/ResponsesTo1Suit/AcolRaiseMajorOver1Suit.cs");
        return (BiddingRuleBase)Activator.CreateInstance(type!)!;
    }

    private static BiddingRuleBase GetNewSuitRule()
    {
        var type = Type.GetType("BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit.AcolNewSuitOver1Suit, BridgeIt.Core");
        Assert.That(type, Is.Not.Null,
            "Rule class AcolNewSuitOver1Suit not found. Create it at BiddingEngine/Rules/Responder/ResponsesTo1Suit/AcolNewSuitOver1Suit.cs");
        return (BiddingRuleBase)Activator.CreateInstance(type!)!;
    }

    private static BiddingRuleBase GetRaiseMinorRule()
    {
        var type = Type.GetType("BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit.AcolRaiseMinorOver1Suit, BridgeIt.Core");
        Assert.That(type, Is.Not.Null,
            "Rule class AcolRaiseMinorOver1Suit not found. Create it at BiddingEngine/Rules/Responder/ResponsesTo1Suit/AcolRaiseMinorOver1Suit.cs");
        return (BiddingRuleBase)Activator.CreateInstance(type!)!;
    }

    private static BiddingRuleBase Get1NTResponseRule()
    {
        var type = Type.GetType("BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit.Acol1NTResponseTo1Suit, BridgeIt.Core");
        Assert.That(type, Is.Not.Null,
            "Rule class Acol1NTResponseTo1Suit not found. Create it at BiddingEngine/Rules/Responder/ResponsesTo1Suit/Acol1NTResponseTo1Suit.cs");
        return (BiddingRuleBase)Activator.CreateInstance(type!)!;
    }
}
