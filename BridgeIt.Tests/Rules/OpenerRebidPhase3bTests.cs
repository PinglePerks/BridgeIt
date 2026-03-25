using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

namespace BridgeIt.Tests.Rules;

/// <summary>
/// Tests for Phase 3b opener rebid rules after partner responds to a 1-suit opening:
///
///   AcolRebidOwnSuit     — rebid own 6+ card suit (priority ~30)
///   AcolRebidNewSuit      — bid a new (second) suit   (priority ~35)
///   AcolRebidBalanced     — rebid 1NT (15-17) or 2NT (18-19) with balanced hand (priority 25, exists)
///   AcolRebidRaiseSuit    — raise partner's suit       (priority ~40)
///
/// All rules: SeatRoleType.Opener, BiddingRound 2, after a 1-suit opening.
/// Partner will have responded with a new suit (1-level or 2-level) or 1NT.
///
/// Expected namespace: BridgeIt.Core.BiddingEngine.Rules.OpenerRebid
/// </summary>
[TestFixture]
public class OpenerRebidPhase3bTests
{
    // =============================================================
    // HELPERS
    // =============================================================

    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    /// <summary>
    /// Creates a context where North opened 1 of openingSuit, East passed,
    /// South responded with responseBid, West passed — North (opener) to rebid.
    /// </summary>
    private static DecisionContext CreateOpenerRebidContext(
        Suit openingSuit, Bid responseBid, int hcp, Dictionary<Suit, int> shape,
        bool isBalanced = false,
        int partnerHcpMin = 6, int partnerHcpMax = 30,
        int partnerSuitMin = 0)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, openingSuit)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, responseBid));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = isBalanced,
            Losers = 7,
            LongestAndStrongest = shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key
        };

        var aucEval = AuctionEvaluator.Evaluate(history);

        var tableKnowledge = new TableKnowledge(Seat.North);
        tableKnowledge.Partner.HcpMin = partnerHcpMin;
        tableKnowledge.Partner.HcpMax = partnerHcpMax;
        if (responseBid.Type == BidType.Suit && responseBid.Suit.HasValue && partnerSuitMin > 0)
            tableKnowledge.Partner.MinShape[responseBid.Suit.Value] = partnerSuitMin;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a wrong-context scenario — it's East's turn (overcaller), not the opener.
    /// </summary>
    private static DecisionContext CreateNotOpenerContext()
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        // Next to bid: East (overcaller position)

        var handEval = new HandEvaluation
        {
            Hcp = 10, Shape = Shape(4, 3, 3, 3), IsBalanced = true, Losers = 7,
            LongestAndStrongest = Suit.Spades
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.East);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.East, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    // =============================================================
    //  1. REBID OWN SUIT
    //     Rule: AcolRebidOwnSuit
    //     Opener opened 1M/1m, partner responded, opener rebids
    //     own suit with 6+ cards.
    //
    //     After partner responds at 1-level or 1NT:
    //       Simple rebid (2-level) = 12-15 HCP, 6+ cards
    //       Jump rebid (3-level)   = 16-18 HCP, 6+ cards (strong, invitational)
    //
    //     This rule should NOT fire with only 5 cards in the suit —
    //     that's a different situation (balanced rebid or new suit).
    // =============================================================

    #region Rebid Own Suit — CouldMakeBid

    [Test]
    public void RebidOwnSuit_CouldMakeBid_TrueWith6CardSuit()
    {
        // Opened 1H, partner responded 1S, we have 6 hearts
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidOwnSuit_CouldMakeBid_TrueWith7CardSuit()
    {
        // Opened 1D, partner responded 1H, we have 7 diamonds
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 12, Shape(2, 2, 7, 2));
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidOwnSuit_CouldMakeBid_FalseWith5CardSuit()
    {
        // Opened 1H with 5 hearts — shouldn't rebid own suit, only 5
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(3, 5, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidOwnSuit_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidOwnSuit_CouldMakeBid_TrueAfterPartner1NT()
    {
        // Partner responded 1NT, we have 6 hearts
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.NoTrumpsBid(1), 13, Shape(2, 6, 3, 2),
            partnerHcpMin: 6, partnerHcpMax: 9);
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.True);
    }

    #endregion

    #region Rebid Own Suit — Apply

    [Test]
    public void RebidOwnSuit_Apply_SimpleRebid2H_With12Hcp()
    {
        // Opened 1H, partner bid 1S, 12 HCP 6 hearts → simple 2H
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 12, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void RebidOwnSuit_Apply_SimpleRebid2H_With15Hcp()
    {
        // 15 HCP still simple rebid
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 15, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void RebidOwnSuit_Apply_JumpRebid3H_With16Hcp()
    {
        // 16 HCP with 6 hearts → jump to 3H (invitational)
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 16, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void RebidOwnSuit_Apply_JumpRebid3D_With18Hcp()
    {
        // 18 HCP with 6 diamonds → jump to 3D
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 18, Shape(2, 3, 6, 2));
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Diamonds)));
    }

    [Test]
    public void RebidOwnSuit_Apply_SimpleRebid2D_AfterPartner1NT()
    {
        // Opened 1D, partner responded 1NT (6-9), we have 6 diamonds, 13 HCP
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.NoTrumpsBid(1), 13, Shape(3, 2, 6, 2),
            partnerHcpMin: 6, partnerHcpMax: 9);
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void RebidOwnSuit_Apply_SimpleRebid2C_With6Clubs()
    {
        // Opened 1C, partner bid 1H, 14 HCP 6 clubs → 2C
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Hearts), 14, Shape(2, 3, 2, 6));
        Assert.That(GetRebidOwnSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Clubs)));
    }

    #endregion

    #region Rebid Own Suit — CouldExplainBid

    [Test]
    public void RebidOwnSuit_CouldExplainBid_TrueFor2H_After1H()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RebidOwnSuit_CouldExplainBid_TrueFor3H_After1H()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RebidOwnSuit_CouldExplainBid_FalseForDifferentSuit()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.False);
    }

    [Test]
    public void RebidOwnSuit_CouldExplainBid_FalseForNT()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(GetRebidOwnSuitRule().CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    #endregion

    #region Rebid Own Suit — GetConstraintForBid

    [Test]
    public void RebidOwnSuit_GetConstraint_2H_Shows6PlusAndMinimum()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        var info = GetRebidOwnSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint, Is.Not.Null);
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
        Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(6));

        var hcpConstraint = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Max, Is.LessThanOrEqualTo(15));
    }

    [Test]
    public void RebidOwnSuit_GetConstraint_3H_Shows6PlusAndExtras()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 17, Shape(2, 6, 3, 2));
        var info = GetRebidOwnSuitRule().GetConstraintForBid(Bid.SuitBid(3, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.GreaterThanOrEqualTo(16));
    }

    #endregion

    // =============================================================
    //  2. REBID NEW SUIT
    //     Rule: AcolRebidNewSuit
    //     Opener opened 1X, partner responded, opener bids a
    //     different (second) suit. Shows 4+ cards in the new suit.
    //
    //     At 1-level (e.g. 1C-1D-1H): 12+ HCP, 4+ cards in new suit
    //     At 2-level without jump (e.g. 1H-1S-2C): 12+ HCP, 4+ cards
    //     Reverse (higher-ranking at 2-level, e.g. 1D-1S-2H): 16+ HCP
    //     Jump shift (e.g. 1H-1S-3C): 19+ HCP, game-forcing
    //
    //     New suit is forcing for one round.
    // =============================================================

    #region Rebid New Suit — CouldMakeBid

    [Test]
    public void RebidNewSuit_CouldMakeBid_TrueWith2Suits()
    {
        // Opened 1H (5), partner responded 1S, have 4 clubs → can bid 2C
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_TrueAt1Level()
    {
        // Opened 1C, partner responded 1D, have 4 hearts → can bid 1H
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Diamonds), 13, Shape(2, 4, 2, 5));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_FalseNoSecondSuit()
    {
        // Opened 1H (5), but no other 4-card suit
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(3, 5, 3, 2));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_FalseForReverseTooWeak()
    {
        // Opened 1D, partner responded 1S, have 4 hearts but only 13 HCP
        // 2H would be a reverse (higher suit at 2-level) — needs 16+ HCP
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 4, 5, 2));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_TrueForReverseWithStrength()
    {
        // Same shape but 16+ HCP — reverse is fine
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Spades), 16, Shape(2, 4, 5, 2));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidNewSuit_CouldMakeBid_FalseWhenOnlySecondSuitIsPartnerssuit()
    {
        // Opened 1D, partner responded 1H, only other 4-card suit is hearts (partner's suit)
        // Bidding partner's suit = raising, not a new suit
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(3, 4, 5, 1));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Rebid New Suit — Apply

    [Test]
    public void RebidNewSuit_Apply_Bids1H_Over1D()
    {
        // Opened 1C, partner responded 1D, 4 hearts → bid 1H (cheapest at 1-level)
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Diamonds), 13, Shape(3, 4, 1, 5));
        Assert.That(GetRebidNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void RebidNewSuit_Apply_Bids1S_Over1D_With4Spades()
    {
        // Opened 1C, partner responded 1D, 4 spades no 4 hearts → bid 1S
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Diamonds), 13, Shape(4, 2, 2, 5));
        Assert.That(GetRebidNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void RebidNewSuit_Apply_Bids2C_After1H_1S()
    {
        // Opened 1H (5), partner responded 1S, 4 clubs → bid 2C
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Clubs)));
    }

    [Test]
    public void RebidNewSuit_Apply_Bids2D_After1S_With4Diamonds()
    {
        // Opened 1S (5), partner responded 1NT, 4 diamonds → bid 2D
        var ctx = CreateOpenerRebidContext(
            Suit.Spades, Bid.NoTrumpsBid(1), 13, Shape(5, 2, 4, 2),
            partnerHcpMin: 6, partnerHcpMax: 9);
        Assert.That(GetRebidNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void RebidNewSuit_Apply_Reverse2H_After1D_1S()
    {
        // Opened 1D (5), partner responded 1S, 4 hearts, 16 HCP → reverse 2H
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Spades), 16, Shape(2, 4, 5, 2));
        Assert.That(GetRebidNewSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void RebidNewSuit_Apply_DoesNotBidPartnerssSuit()
    {
        // Opened 1D (5), partner responded 1H, we have 4 spades → bid 1S, not hearts
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(4, 2, 5, 2));
        var bid = GetRebidNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.Not.Null);
        Assert.That(bid!.Suit, Is.Not.EqualTo(Suit.Hearts), "Should not bid partner's suit");
        Assert.That(bid!.Suit, Is.Not.EqualTo(Suit.Diamonds), "Should not rebid own opening suit");
    }

    [Test]
    public void RebidNewSuit_Apply_BidsLowerSuitAt2Level()
    {
        // Opened 1S (5), partner responded 1NT, have 4D and 4C
        // At 2-level: bid lower-ranking suit first (2C before 2D)
        var ctx = CreateOpenerRebidContext(
            Suit.Spades, Bid.NoTrumpsBid(1), 13, Shape(5, 2, 4, 4),
            isBalanced: false, partnerHcpMin: 6, partnerHcpMax: 9);
        var bid = GetRebidNewSuitRule().Apply(ctx);
        Assert.That(bid, Is.Not.Null);
        Assert.That(bid!.Suit, Is.EqualTo(Suit.Clubs));
    }

    #endregion

    #region Rebid New Suit — CouldExplainBid

    [Test]
    public void RebidNewSuit_CouldExplainBid_TrueForNewSuitAtCorrectLevel()
    {
        // After 1H-1S, could explain 2C or 2D
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.True);
    }

    [Test]
    public void RebidNewSuit_CouldExplainBid_FalseForOpeningSuit()
    {
        // Rebidding own suit is not a new suit
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    [Test]
    public void RebidNewSuit_CouldExplainBid_FalseForPartnersSuit()
    {
        // Raising partner's suit is not a new suit
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.False);
    }

    [Test]
    public void RebidNewSuit_CouldExplainBid_FalseForNT()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        Assert.That(GetRebidNewSuitRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    #endregion

    #region Rebid New Suit — GetConstraintForBid

    [Test]
    public void RebidNewSuit_GetConstraint_NonReverse_Shows4PlusCards()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        var info = GetRebidNewSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Clubs), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint, Is.Not.Null);
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Clubs));
        Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void RebidNewSuit_GetConstraint_Reverse_Shows16PlusHcp()
    {
        // A reverse bid (higher-ranking suit at 2-level) shows 16+ HCP
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Spades), 17, Shape(2, 4, 5, 2));
        var info = GetRebidNewSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.GreaterThanOrEqualTo(16));
    }

    [Test]
    public void RebidNewSuit_GetConstraint_ForwardState_ConstructiveSearch()
    {
        // New suit is forcing — partnership is still searching
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 2, 4));
        var info = GetRebidNewSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Clubs), ctx);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.ConstructiveSearch));
    }

    #endregion

    // =============================================================
    //  3. REBID BALANCED (NT)
    //     Rule: AcolRebidBalanced (already exists at priority 25)
    //     Opener opened 1-suit, partner responded, opener rebids NT
    //     with balanced hand.
    //
    //     1NT rebid = 15-17 HCP, balanced (too strong for 1NT opening)
    //     2NT rebid = 18-19 HCP, balanced
    //
    //     Should NOT fire with < 15 HCP balanced (already opened 1NT)
    //     or with unbalanced hands.
    // =============================================================

    #region Rebid Balanced — CouldMakeBid

    [Test]
    public void RebidBalanced_CouldMakeBid_TrueWith15HcpBalanced()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 15, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidBalanced_CouldMakeBid_TrueWith18HcpBalanced()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 18, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RebidBalanced_CouldMakeBid_FalseUnbalanced()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 16, Shape(2, 6, 3, 2),
            isBalanced: false);
        Assert.That(GetRebidBalancedRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RebidBalanced_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetRebidBalancedRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Rebid Balanced — Apply

    [Test]
    public void RebidBalanced_Apply_1NT_With15Hcp()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 15, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void RebidBalanced_Apply_1NT_With17Hcp()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 17, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(1)));
    }

    [Test]
    public void RebidBalanced_Apply_2NT_With18Hcp()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 18, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void RebidBalanced_Apply_2NT_With19Hcp()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 19, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(2)));
    }

    [Test]
    public void RebidBalanced_Apply_Null_With12HcpBalanced()
    {
        // 12-14 balanced should have opened 1NT, not reached here.
        // If it does, Apply should return null (no valid NT rebid).
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 12, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().Apply(ctx), Is.Null);
    }

    #endregion

    #region Rebid Balanced — CouldExplainBid

    [Test]
    public void RebidBalanced_CouldExplainBid_TrueFor1NT()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 16, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.True);
    }

    [Test]
    public void RebidBalanced_CouldExplainBid_TrueFor2NT()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 18, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.True);
    }

    [Test]
    public void RebidBalanced_CouldExplainBid_FalseForSuitBid()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 16, Shape(3, 4, 3, 3),
            isBalanced: true);
        Assert.That(GetRebidBalancedRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    #endregion

    #region Rebid Balanced — GetConstraintForBid

    [Test]
    public void RebidBalanced_GetConstraint_1NT_Shows15to17()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 16, Shape(3, 4, 3, 3),
            isBalanced: true);
        var info = GetRebidBalancedRule().GetConstraintForBid(Bid.NoTrumpsBid(1), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.EqualTo(15));
        Assert.That(hcpConstraint.Max, Is.EqualTo(17));

        var balancedConstraint = composite.Constraints.OfType<BalancedConstraint>().FirstOrDefault();
        Assert.That(balancedConstraint, Is.Not.Null);
    }

    [Test]
    public void RebidBalanced_GetConstraint_2NT_Shows18to19()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 18, Shape(3, 4, 3, 3),
            isBalanced: true);
        var info = GetRebidBalancedRule().GetConstraintForBid(Bid.NoTrumpsBid(2), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.EqualTo(18));
        Assert.That(hcpConstraint.Max, Is.EqualTo(19));
    }

    #endregion

    // =============================================================
    //  4. RAISE PARTNER'S SUIT
    //     Rule: AcolRebidRaiseSuit
    //     Opener opened 1X, partner responded in a new suit,
    //     opener raises partner's suit with 4+ card support.
    //
    //     Simple raise (2-level): 12-15 HCP, 4+ support
    //     Jump raise (3-level):   16-18 HCP, 4+ support (invitational)
    //     Game raise (4-level):   19+ HCP, 4+ support (major game)
    //
    //     Only applies when partner bid a suit (not 1NT).
    // =============================================================

    #region Raise Partner's Suit — CouldMakeBid

    [Test]
    public void RaiseSuit_CouldMakeBid_TrueWith4CardSupport()
    {
        // Opened 1D, partner responded 1H, we have 4 hearts
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void RaiseSuit_CouldMakeBid_FalseWithOnly3CardSupport()
    {
        // Only 3 hearts — not enough to raise
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(3, 3, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseSuit_CouldMakeBid_FalseAfterPartner1NT()
    {
        // Partner responded 1NT — no suit to raise
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.NoTrumpsBid(1), 13, Shape(2, 5, 3, 3),
            partnerHcpMin: 6, partnerHcpMax: 9);
        Assert.That(GetRebidRaiseSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void RaiseSuit_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetRebidRaiseSuitRule().CouldMakeBid(ctx), Is.False);
    }

    #endregion

    #region Raise Partner's Suit — Apply

    [Test]
    public void RaiseSuit_Apply_SimpleRaise2H_With13Hcp()
    {
        // Opened 1D, partner responded 1H, 13 HCP, 4 hearts → 2H
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void RaiseSuit_Apply_SimpleRaise2H_With15Hcp()
    {
        // 15 HCP still simple raise
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 15, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void RaiseSuit_Apply_JumpRaise3H_With16Hcp()
    {
        // 16 HCP → jump raise 3H (invitational)
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 16, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void RaiseSuit_Apply_JumpRaise3S_With17Hcp()
    {
        // Opened 1C, partner responded 1S, 17 HCP, 4 spades → 3S
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Spades), 17, Shape(4, 2, 2, 5),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void RaiseSuit_Apply_GameRaise4H_With19Hcp()
    {
        // 19 HCP → game 4H
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 19, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Hearts)));
    }

    [Test]
    public void RaiseSuit_Apply_GameRaise4S_With20Hcp()
    {
        // Opened 1C, partner responded 1S, 20 HCP, 4 spades → 4S
        var ctx = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Spades), 20, Shape(4, 2, 2, 5),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Spades)));
    }

    [Test]
    public void RaiseSuit_Apply_SimpleRaise2S_AfterPartner1S()
    {
        // Opened 1H, partner responded 1S, 14 HCP, 4 spades → 2S
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 14, Shape(4, 5, 2, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    #endregion

    #region Raise Partner's Suit — CouldExplainBid

    [Test]
    public void RaiseSuit_CouldExplainBid_TrueForRaise2H()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseSuit_CouldExplainBid_TrueForJumpRaise3H()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseSuit_CouldExplainBid_TrueForGameRaise4H()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.SuitBid(4, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void RaiseSuit_CouldExplainBid_FalseForDifferentSuit()
    {
        // Raising diamonds when partner bid hearts — no
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.False);
    }

    [Test]
    public void RaiseSuit_CouldExplainBid_FalseForNT()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void RaiseSuit_CouldExplainBid_FalseForPass()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldExplainBid(Bid.Pass(), ctx), Is.False);
    }

    #endregion

    #region Raise Partner's Suit — GetConstraintForBid

    [Test]
    public void RaiseSuit_GetConstraint_2H_Shows4PlusSupportAndMinimum()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        var info = GetRebidRaiseSuitRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var suitConstraint = composite!.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
        Assert.That(suitConstraint, Is.Not.Null);
        Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
        Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));

        var hcpConstraint = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.EqualTo(12));
        Assert.That(hcpConstraint.Max, Is.LessThanOrEqualTo(15));

        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.FitEstablished));
    }

    [Test]
    public void RaiseSuit_GetConstraint_3H_ShowsInviteStrength()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 17, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        var info = GetRebidRaiseSuitRule().GetConstraintForBid(Bid.SuitBid(3, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.GreaterThanOrEqualTo(16));
        Assert.That(hcpConstraint.Max, Is.LessThanOrEqualTo(18));

        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.GameInvitational));
    }

    [Test]
    public void RaiseSuit_GetConstraint_4H_ShowsGameStrength()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 19, Shape(2, 4, 5, 2),
            partnerSuitMin: 4);
        var info = GetRebidRaiseSuitRule().GetConstraintForBid(Bid.SuitBid(4, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        var composite = info!.Constraint as CompositeConstraint;
        Assert.That(composite, Is.Not.Null);

        var hcpConstraint = composite!.Constraints.OfType<HcpConstraint>().FirstOrDefault();
        Assert.That(hcpConstraint, Is.Not.Null);
        Assert.That(hcpConstraint!.Min, Is.GreaterThanOrEqualTo(19));

        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    #endregion

    // =============================================================
    //  EDGE CASES & CROSS-RULE SCENARIOS
    // =============================================================

    #region Edge Cases

    [Test]
    public void EdgeCase_AllRebidRules_VerifyBiddingRoundIs2()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(ctx.AuctionEvaluation.BiddingRound, Is.EqualTo(2));
        Assert.That(ctx.AuctionEvaluation.SeatRoleType, Is.EqualTo(SeatRoleType.Opener));
    }

    [Test]
    public void EdgeCase_AllRebidRules_VerifyMyLastBidIsOpening()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(ctx.AuctionEvaluation.MyLastNonPassBid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void EdgeCase_AllRebidRules_VerifyPartnerBidIsResponse()
    {
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 6, 3, 2));
        Assert.That(ctx.AuctionEvaluation.PartnerLastNonPassBid, Is.EqualTo(Bid.SuitBid(1, Suit.Spades)));
    }

    [Test]
    public void EdgeCase_RebidOwnSuit_DoesNotFireWith5Cards()
    {
        // 5 hearts is enough to open, but not enough to rebid the suit
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 15, Shape(3, 5, 3, 2),
            isBalanced: true);
        Assert.That(GetRebidOwnSuitRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void EdgeCase_RaiseSuit_WithMinorSupport()
    {
        // Opened 1H, partner responded 2C (10+ HCP, 4+ clubs)
        // We have 4 clubs — can raise to 3C
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(2, Suit.Clubs), 13, Shape(2, 5, 2, 4),
            partnerHcpMin: 10, partnerHcpMax: 30, partnerSuitMin: 4);
        Assert.That(GetRebidRaiseSuitRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void EdgeCase_RebidNewSuit_ExcludesOpeningSuit()
    {
        // Opened 1H, partner responded 1S. Rebid new suit should not rebid hearts.
        var ctx = CreateOpenerRebidContext(
            Suit.Hearts, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 5, 4, 2));
        var bid = GetRebidNewSuitRule().Apply(ctx);
        if (bid != null)
        {
            Assert.That(bid.Suit, Is.Not.EqualTo(Suit.Hearts), "New suit should not be the opening suit");
        }
    }

    [Test]
    public void EdgeCase_RebidNewSuit_ExcludesPartnersSuit()
    {
        // Opened 1D, partner responded 1H. Second suit = spades. Should not bid hearts.
        var ctx = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Hearts), 13, Shape(4, 2, 5, 2));
        var bid = GetRebidNewSuitRule().Apply(ctx);
        if (bid != null)
        {
            Assert.That(bid.Suit, Is.Not.EqualTo(Suit.Hearts), "New suit should not be partner's suit");
            Assert.That(bid.Suit, Is.Not.EqualTo(Suit.Diamonds), "New suit should not be opening suit");
        }
    }

    [Test]
    public void EdgeCase_RebidNewSuit_ReverseDefinition()
    {
        // A reverse = bidding a HIGHER-ranking suit at the 2-level than the opening suit
        // 1C-1S-2D is NOT a reverse (diamonds > clubs, but clubs is lowest)
        // 1D-1S-2H IS a reverse (hearts > diamonds)
        // 1C-1D-1H is NOT a reverse (it's at 1-level)

        // This is NOT a reverse: 1C, partner 1S, bid 2D (new lower suit at 2-level)
        var ctxNotReverse = CreateOpenerRebidContext(
            Suit.Clubs, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 2, 4, 5));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctxNotReverse), Is.True,
            "2D over 1C is not a reverse — 12 HCP should suffice");

        // This IS a reverse: 1D, partner 1S, bid 2H (higher-ranking new suit at 2-level)
        var ctxReverse = CreateOpenerRebidContext(
            Suit.Diamonds, Bid.SuitBid(1, Suit.Spades), 13, Shape(2, 4, 5, 2));
        Assert.That(GetRebidNewSuitRule().CouldMakeBid(ctxReverse), Is.False,
            "2H over 1D is a reverse — needs 16+ HCP");
    }

    #endregion

    // =============================================================
    // RULE FACTORY METHODS
    // Expected location: BridgeIt.Core/BiddingEngine/Rules/OpenerRebid/
    // =============================================================

    private static BiddingRuleBase GetRebidOwnSuitRule()
        => new AcolRebidOwnSuit();

    private static BiddingRuleBase GetRebidNewSuitRule()
        => new AcolRebidNewSuit();

    private static BiddingRuleBase GetRebidBalancedRule()
        => new AcolRebidBalanced();

    private static BiddingRuleBase GetRebidRaiseSuitRule()
        => new AcolRebidRaiseSuit();
}
