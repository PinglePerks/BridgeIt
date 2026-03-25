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
/// Tests for Phase 3a opener rebid rules:
///   AcolStaymanResponse        — opener responds to Stayman 2C after 1NT opening
///   AcolOpenerAfterNTInvite    — opener accepts/rejects 2NT invite after 1NT opening
///   AcolOpenerAfterMajorRaise  — opener decides level after responder raises major
///
/// Expected namespace: BridgeIt.Core.BiddingEngine.Rules.OpenerRebid
/// </summary>
[TestFixture]
public class OpenerRebidPhase3aTests
{
    // =============================================================
    // HELPERS
    // =============================================================

    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) =>
        new() { { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c } };

    /// <summary>
    /// Creates a context where North opened 1NT, East passed, South bid Stayman 2C,
    /// West passed — North (opener) to rebid.
    /// </summary>
    private static DecisionContext CreateStaymanResponseContext(
        int hcp, Dictionary<Suit, int> shape)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Clubs))); // Stayman
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = true,
            Losers = 7,
            LongestAndStrongest = shape.OrderByDescending(kv => kv.Value)
                .ThenByDescending(kv => kv.Key).First().Key
        };

        var aucEval = AuctionEvaluator.Evaluate(history);

        // Partner (South) bid Stayman — we know they have 11+ HCP and a 4-card major
        var tableKnowledge = new TableKnowledge(Seat.North);
        tableKnowledge.Partner.HcpMin = 11;
        tableKnowledge.Partner.HcpMax = 30;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a context where North opened 1NT, East passed, South bid 2NT (invite),
    /// West passed — North (opener) to decide.
    /// </summary>
    private static DecisionContext CreateNTInviteContext(int hcp)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(2))); // 2NT invite
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var shape = Shape(3, 3, 4, 3); // balanced
        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = true,
            Losers = 7,
            LongestAndStrongest = Suit.Diamonds
        };

        var aucEval = AuctionEvaluator.Evaluate(history);

        // Partner showed 11-12 HCP with 2NT invite
        var tableKnowledge = new TableKnowledge(Seat.North);
        tableKnowledge.Partner.HcpMin = 12;
        tableKnowledge.Partner.HcpMax = 12;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a context where North opened 1 of a major, East passed,
    /// South raised to the given level, West passed — North (opener) to rebid.
    /// </summary>
    private static DecisionContext CreateAfterMajorRaiseContext(
        Suit openingSuit, int raiseLevel, int hcp, Dictionary<Suit, int> shape)
    {
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, openingSuit)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(raiseLevel, openingSuit))); // raise
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = hcp,
            Shape = shape,
            IsBalanced = false,
            Losers = 7,
            LongestAndStrongest = openingSuit
        };

        var aucEval = AuctionEvaluator.Evaluate(history);

        var tableKnowledge = new TableKnowledge(Seat.North);
        // Set partner's known range based on raise level
        if (raiseLevel == 2)
        {
            tableKnowledge.Partner.HcpMin = 6;
            tableKnowledge.Partner.HcpMax = 9;
        }
        else if (raiseLevel == 3)
        {
            tableKnowledge.Partner.HcpMin = 10;
            tableKnowledge.Partner.HcpMax = 12;
        }
        tableKnowledge.Partner.MinShape[openingSuit] = 4;

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    /// <summary>
    /// Creates a wrong-context scenario — it's East's turn (overcaller position),
    /// so this player is not the opener.
    /// North opened 1H, now it's East's turn to bid.
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
    //  1. STAYMAN RESPONSE
    //     Rule: AcolStaymanResponse
    //     Opener opened 1NT, partner bid Stayman 2C.
    //     Opener responds:
    //       2D = no 4-card major
    //       2H = 4+ hearts (bid hearts even if also have 4 spades)
    //       2S = 4+ spades (no 4 hearts)
    // =============================================================

    #region Stayman Response — CouldMakeBid

    [Test]
    public void StaymanResponse_CouldMakeBid_TrueAfter1NT_2C()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(3, 4, 3, 3));
        Assert.That(GetStaymanResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void StaymanResponse_CouldMakeBid_TrueWithNoMajor()
    {
        // Even without a 4-card major, rule fires — bids 2D
        var ctx = CreateStaymanResponseContext(13, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void StaymanResponse_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetStaymanResponseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void StaymanResponse_CouldMakeBid_FalseAfter1SuitOpening()
    {
        // Partner bid 2C over a 1-suit opening — that's not Stayman
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Clubs)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = Shape(3, 5, 3, 2), IsBalanced = false, Losers = 6,
            LongestAndStrongest = Suit.Hearts
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetStaymanResponseRule().CouldMakeBid(decCtx), Is.False);
    }

    [Test]
    public void StaymanResponse_CouldMakeBid_FalseOnRound1()
    {
        // If it's still round 1 for opener, they haven't heard Stayman yet
        var history = new AuctionHistory(Seat.North);
        // Only 1NT has been bid — no response yet
        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = Shape(3, 4, 3, 3), IsBalanced = true, Losers = 7,
            LongestAndStrongest = Suit.Hearts
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetStaymanResponseRule().CouldMakeBid(decCtx), Is.False);
    }

    #endregion

    #region Stayman Response — Apply

    [Test]
    public void StaymanResponse_Apply_Bids2H_With4Hearts()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(3, 4, 3, 3));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void StaymanResponse_Apply_Bids2S_With4SpadesNo4Hearts()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(4, 3, 3, 3));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Spades)));
    }

    [Test]
    public void StaymanResponse_Apply_Bids2H_WithBothMajors()
    {
        // With 4H and 4S, bid 2H first — responder can then bid 2S if they have spades
        var ctx = CreateStaymanResponseContext(13, Shape(4, 4, 3, 2));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void StaymanResponse_Apply_Bids2D_WithNoMajor()
    {
        var ctx = CreateStaymanResponseContext(12, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void StaymanResponse_Apply_Bids2D_With3Hearts3Spades()
    {
        var ctx = CreateStaymanResponseContext(14, Shape(3, 3, 3, 4));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Diamonds)));
    }

    [Test]
    public void StaymanResponse_Apply_Bids2H_With5HeartsAnd4Spades()
    {
        // 5 hearts still shows hearts first
        var ctx = CreateStaymanResponseContext(13, Shape(4, 5, 2, 2));
        Assert.That(GetStaymanResponseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    #endregion

    #region Stayman Response — CouldExplainBid

    [Test]
    public void StaymanResponse_CouldExplainBid_TrueFor2D()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.SuitBid(2, Suit.Diamonds), ctx), Is.True);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_TrueFor2H()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_TrueFor2S()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.SuitBid(2, Suit.Spades), ctx), Is.True);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_FalseFor2C()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.SuitBid(2, Suit.Clubs), ctx), Is.False);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_FalseFor1NT()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.NoTrumpsBid(1), ctx), Is.False);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_FalseFor3Level()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.False);
    }

    [Test]
    public void StaymanResponse_CouldExplainBid_FalseForPass()
    {
        var ctx = CreateStaymanResponseContext(0, Shape(3, 3, 4, 3));
        Assert.That(GetStaymanResponseRule().CouldExplainBid(Bid.Pass(), ctx), Is.False);
    }

    #endregion

    #region Stayman Response — GetConstraintForBid

    [Test]
    public void StaymanResponse_GetConstraint_2D_NoMajor()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(3, 3, 4, 3));
        var info = GetStaymanResponseRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Diamonds), ctx);

        Assert.That(info, Is.Not.Null);
        // 2D denies a 4-card major — constraint should indicate hearts < 4 and spades < 4
        // At minimum, it's a valid BidInformation with balanced constraint
        Assert.That(info!.Constraint, Is.Not.Null);
    }

    [Test]
    public void StaymanResponse_GetConstraint_2H_Shows4Hearts()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(3, 4, 3, 3));
        var info = GetStaymanResponseRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);

        // Should contain a suit length constraint showing 4+ hearts
        var composite = info!.Constraint as CompositeConstraint;
        if (composite != null)
        {
            var suitConstraint = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
            Assert.That(suitConstraint, Is.Not.Null);
            Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
            Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));
        }
        else
        {
            // Could be a single SuitLengthConstraint
            var suitConstraint = info.Constraint as SuitLengthConstraint;
            Assert.That(suitConstraint, Is.Not.Null);
            Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Hearts));
            Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));
        }
    }

    [Test]
    public void StaymanResponse_GetConstraint_2S_Shows4Spades()
    {
        var ctx = CreateStaymanResponseContext(13, Shape(4, 3, 3, 3));
        var info = GetStaymanResponseRule().GetConstraintForBid(Bid.SuitBid(2, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);

        var composite = info!.Constraint as CompositeConstraint;
        if (composite != null)
        {
            var suitConstraint = composite.Constraints.OfType<SuitLengthConstraint>().FirstOrDefault();
            Assert.That(suitConstraint, Is.Not.Null);
            Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Spades));
            Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));
        }
        else
        {
            var suitConstraint = info.Constraint as SuitLengthConstraint;
            Assert.That(suitConstraint, Is.Not.Null);
            Assert.That(suitConstraint!.Suit, Is.EqualTo(Suit.Spades));
            Assert.That(suitConstraint.MinLen, Is.GreaterThanOrEqualTo(4));
        }
    }

    #endregion

    // =============================================================
    //  2. OPENER AFTER NT INVITE
    //     Rule: AcolOpenerAfterNTInvite
    //     Opener opened 1NT (12-14), partner bid 2NT (11-12 invite).
    //     Opener decides:
    //       Pass  = 12 HCP (combined max 24, can't make game)
    //       3NT   = 13-14 HCP (combined min 24-25, bid game)
    // =============================================================

    #region NT Invite — CouldMakeBid

    [Test]
    public void NTInvite_CouldMakeBid_TrueAfter1NT_2NT()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(GetNTInviteRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTInvite_CouldMakeBid_TrueWithMinimum()
    {
        // Even with 12 HCP — rule fires, decides to pass
        var ctx = CreateNTInviteContext(12);
        Assert.That(GetNTInviteRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void NTInvite_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetNTInviteRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void NTInvite_CouldMakeBid_FalseAfter1SuitOpening()
    {
        // Opened 1H, partner bid 2NT — this is a different situation (natural 2NT response)
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.NoTrumpsBid(2)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 14, Shape = Shape(3, 5, 3, 2), IsBalanced = false, Losers = 6,
            LongestAndStrongest = Suit.Hearts
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetNTInviteRule().CouldMakeBid(decCtx), Is.False);
    }

    #endregion

    #region NT Invite — Apply

    [Test]
    public void NTInvite_Apply_Passes_With12Hcp()
    {
        var ctx = CreateNTInviteContext(12);
        Assert.That(GetNTInviteRule().Apply(ctx), Is.EqualTo(Bid.Pass()));
    }

    [Test]
    public void NTInvite_Apply_Bids3NT_With13Hcp()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(GetNTInviteRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    [Test]
    public void NTInvite_Apply_Bids3NT_With14Hcp()
    {
        var ctx = CreateNTInviteContext(14);
        Assert.That(GetNTInviteRule().Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    #endregion

    #region NT Invite — CouldExplainBid

    [Test]
    public void NTInvite_CouldExplainBid_TrueForPass()
    {
        var ctx = CreateNTInviteContext(12);
        Assert.That(GetNTInviteRule().CouldExplainBid(Bid.Pass(), ctx), Is.True);
    }

    [Test]
    public void NTInvite_CouldExplainBid_TrueFor3NT()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(GetNTInviteRule().CouldExplainBid(Bid.NoTrumpsBid(3), ctx), Is.True);
    }

    [Test]
    public void NTInvite_CouldExplainBid_FalseFor2NT()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(GetNTInviteRule().CouldExplainBid(Bid.NoTrumpsBid(2), ctx), Is.False);
    }

    [Test]
    public void NTInvite_CouldExplainBid_FalseForSuitBid()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(GetNTInviteRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.False);
    }

    #endregion

    #region NT Invite — GetConstraintForBid

    [Test]
    public void NTInvite_GetConstraint_Pass_Hcp12()
    {
        var ctx = CreateNTInviteContext(12);
        var info = GetNTInviteRule().GetConstraintForBid(Bid.Pass(), ctx);

        Assert.That(info, Is.Not.Null);

        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(12));
        Assert.That(hcp.Max, Is.EqualTo(12));
        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    [Test]
    public void NTInvite_GetConstraint_3NT_Hcp13to14()
    {
        var ctx = CreateNTInviteContext(13);
        var info = GetNTInviteRule().GetConstraintForBid(Bid.NoTrumpsBid(3), ctx);

        Assert.That(info, Is.Not.Null);

        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(13));
        Assert.That(hcp.Max, Is.EqualTo(14));
        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    #endregion

    // =============================================================
    //  3. OPENER AFTER MAJOR RAISE
    //     Rule: AcolOpenerAfterMajorRaise
    //     Opener opened 1H/1S, partner raised to 2M (6-9) or 3M (10-12).
    //     Fit is established — purely a level decision.
    //
    //     After 2M raise (partner: 6-9):
    //       Pass = 12-14 HCP (combined max 23, no game)
    //       3M   = 15-16 HCP (invite — combined 21-25, straddles)
    //       4M   = 17+ HCP  (combined min 23+, bid game)
    //
    //     After 3M raise (partner: 10-12):
    //       Pass = 12-14 HCP (combined max 26, borderline — conservative pass)
    //       4M   = 15+ HCP  (combined min 25, game)
    // =============================================================

    #region After Major Raise — CouldMakeBid

    [Test]
    public void AfterMajorRaise_CouldMakeBid_TrueAfter2H()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 13, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldMakeBid_TrueAfter3S()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 15, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldMakeBid(ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldMakeBid_FalseWhenNotOpener()
    {
        var ctx = CreateNotOpenerContext();
        Assert.That(GetAfterMajorRaiseRule().CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void AfterMajorRaise_CouldMakeBid_FalseAfterMinorRaise()
    {
        // Partner raised our 1D to 2D — this rule handles majors only
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 15, Shape = Shape(3, 2, 5, 3), IsBalanced = false, Losers = 6,
            LongestAndStrongest = Suit.Diamonds
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        tableKnowledge.Partner.HcpMin = 6;
        tableKnowledge.Partner.HcpMax = 9;
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetAfterMajorRaiseRule().CouldMakeBid(decCtx), Is.False);
    }

    [Test]
    public void AfterMajorRaise_CouldMakeBid_FalseAfterNewSuitResponse()
    {
        // Partner bid 1S over our 1H — that's a new suit, not a raise
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.SuitBid(1, Suit.Hearts)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(1, Suit.Spades)));
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 14, Shape = Shape(3, 5, 3, 2), IsBalanced = false, Losers = 6,
            LongestAndStrongest = Suit.Hearts
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetAfterMajorRaiseRule().CouldMakeBid(decCtx), Is.False);
    }

    #endregion

    #region After Major Raise — Apply (after simple raise 2M)

    [Test]
    [TestCase(12, "Pass", Description = "Minimum opener, pass")]
    [TestCase(13, "Pass", Description = "13 HCP, combined max 22, pass")]
    [TestCase(14, "Pass", Description = "14 HCP, combined max 23, pass")]
    public void AfterMajorRaise_Apply_After2H_PassWithMinimum(int hcp, string expected)
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, hcp, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx)!.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(15, "3H", Description = "15 HCP invite, combined 21-24")]
    [TestCase(16, "3H", Description = "16 HCP invite, combined 22-25")]
    public void AfterMajorRaise_Apply_After2H_InviteWithExtras(int hcp, string expected)
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, hcp, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx)!.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(17, "4H", Description = "17 HCP, combined min 23, game")]
    [TestCase(19, "4H", Description = "19 HCP, combined min 25, game")]
    public void AfterMajorRaise_Apply_After2H_GameWithStrong(int hcp, string expected)
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, hcp, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx)!.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void AfterMajorRaise_Apply_After2S_PassWithMinimum()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 2, 13, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx), Is.EqualTo(Bid.Pass()));
    }

    [Test]
    public void AfterMajorRaise_Apply_After2S_InviteWith16()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 2, 16, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Spades)));
    }

    [Test]
    public void AfterMajorRaise_Apply_After2S_GameWith17()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 2, 17, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Spades)));
    }

    #endregion

    #region After Major Raise — Apply (after limit raise 3M)

    [Test]
    [TestCase(12, "Pass", Description = "12 HCP, combined max 24, pass")]
    [TestCase(13, "Pass", Description = "13 HCP, combined max 25, borderline pass")]
    [TestCase(14, "Pass", Description = "14 HCP, combined max 26, conservative pass")]
    public void AfterMajorRaise_Apply_After3H_PassWithMinimum(int hcp, string expected)
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 3, hcp, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx)!.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(15, "4H", Description = "15 HCP, combined min 25, game")]
    [TestCase(17, "4H", Description = "17 HCP, combined min 27, game")]
    [TestCase(19, "4H", Description = "19 HCP, combined min 29, game")]
    public void AfterMajorRaise_Apply_After3H_GameWithExtras(int hcp, string expected)
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 3, hcp, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx)!.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void AfterMajorRaise_Apply_After3S_PassWith13()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 13, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx), Is.EqualTo(Bid.Pass()));
    }

    [Test]
    public void AfterMajorRaise_Apply_After3S_GameWith15()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 15, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Spades)));
    }

    #endregion

    #region After Major Raise — CouldExplainBid

    [Test]
    public void AfterMajorRaise_CouldExplainBid_TrueForPass_After2H()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 0, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.Pass(), ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_TrueFor3H_After2H()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 0, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.SuitBid(3, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_TrueFor4H_After2H()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 0, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.SuitBid(4, Suit.Hearts), ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_TrueForPass_After3S()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 0, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.Pass(), ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_TrueFor4S_After3S()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 0, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.SuitBid(4, Suit.Spades), ctx), Is.True);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_FalseForWrongSuit()
    {
        // After 2H raise, 3S is not a valid rebid for this rule
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 0, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.SuitBid(3, Suit.Spades), ctx), Is.False);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_FalseForNTBid()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 0, Shape(3, 5, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.NoTrumpsBid(3), ctx), Is.False);
    }

    [Test]
    public void AfterMajorRaise_CouldExplainBid_FalseFor3S_After3S_NotAnOption()
    {
        // After a limit raise to 3S, rebidding 3S isn't meaningful (already at 3)
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 0, Shape(5, 3, 3, 2));
        Assert.That(GetAfterMajorRaiseRule().CouldExplainBid(Bid.SuitBid(3, Suit.Spades), ctx), Is.False);
    }

    #endregion

    #region After Major Raise — GetConstraintForBid

    [Test]
    public void AfterMajorRaise_GetConstraint_Pass_After2H_MinimumOpener()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 13, Shape(3, 5, 3, 2));
        var info = GetAfterMajorRaiseRule().GetConstraintForBid(Bid.Pass(), ctx);

        Assert.That(info, Is.Not.Null);

        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.EqualTo(12));
        Assert.That(hcp.Max, Is.LessThanOrEqualTo(15));

        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    [Test]
    public void AfterMajorRaise_GetConstraint_3H_After2H_Invite()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 16, Shape(3, 5, 3, 2));
        var info = GetAfterMajorRaiseRule().GetConstraintForBid(Bid.SuitBid(3, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.GameInvitational));
    }

    [Test]
    public void AfterMajorRaise_GetConstraint_4H_After2H_Game()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 17, Shape(3, 5, 3, 2));
        var info = GetAfterMajorRaiseRule().GetConstraintForBid(Bid.SuitBid(4, Suit.Hearts), ctx);

        Assert.That(info, Is.Not.Null);

        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.GreaterThanOrEqualTo(17));

        Assert.That(info.PartnershipBiddingState, Is.EqualTo(PartnershipBiddingState.SignOff));
    }

    [Test]
    public void AfterMajorRaise_GetConstraint_4S_After3S_Game()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Spades, 3, 15, Shape(5, 3, 3, 2));
        var info = GetAfterMajorRaiseRule().GetConstraintForBid(Bid.SuitBid(4, Suit.Spades), ctx);

        Assert.That(info, Is.Not.Null);

        HcpConstraint? hcp = info!.Constraint as HcpConstraint;
        if (hcp == null && info.Constraint is CompositeConstraint composite)
            hcp = composite.Constraints.OfType<HcpConstraint>().FirstOrDefault();

        Assert.That(hcp, Is.Not.Null);
        Assert.That(hcp!.Min, Is.GreaterThanOrEqualTo(15));
    }

    #endregion

    // =============================================================
    //  EDGE CASES & CROSS-RULE SCENARIOS
    // =============================================================

    #region Edge Cases

    [Test]
    public void EdgeCase_StaymanResponse_VerifyBiddingRoundIs2()
    {
        // Opener's rebid after Stayman should be BiddingRound 2
        var ctx = CreateStaymanResponseContext(13, Shape(3, 4, 3, 3));
        Assert.That(ctx.AuctionEvaluation.BiddingRound, Is.EqualTo(2));
        Assert.That(ctx.AuctionEvaluation.SeatRoleType, Is.EqualTo(SeatRoleType.Opener));
    }

    [Test]
    public void EdgeCase_NTInvite_VerifyBiddingRoundIs2()
    {
        var ctx = CreateNTInviteContext(13);
        Assert.That(ctx.AuctionEvaluation.BiddingRound, Is.EqualTo(2));
        Assert.That(ctx.AuctionEvaluation.SeatRoleType, Is.EqualTo(SeatRoleType.Opener));
    }

    [Test]
    public void EdgeCase_AfterMajorRaise_VerifyBiddingRoundIs2()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 13, Shape(3, 5, 3, 2));
        Assert.That(ctx.AuctionEvaluation.BiddingRound, Is.EqualTo(2));
        Assert.That(ctx.AuctionEvaluation.SeatRoleType, Is.EqualTo(SeatRoleType.Opener));
    }

    [Test]
    public void EdgeCase_AfterMajorRaise_MyLastNonPassBidIsOpening()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 13, Shape(3, 5, 3, 2));
        Assert.That(ctx.AuctionEvaluation.MyLastNonPassBid, Is.EqualTo(Bid.SuitBid(1, Suit.Hearts)));
    }

    [Test]
    public void EdgeCase_AfterMajorRaise_PartnerLastNonPassBidIsRaise()
    {
        var ctx = CreateAfterMajorRaiseContext(Suit.Hearts, 2, 13, Shape(3, 5, 3, 2));
        Assert.That(ctx.AuctionEvaluation.PartnerLastNonPassBid, Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void EdgeCase_StaymanResponse_DoesNotFireAfterTransfer()
    {
        // Partner bid 2D (transfer), not 2C (Stayman) — Stayman response should not fire
        var history = new AuctionHistory(Seat.North);
        history.Add(new AuctionBid(Seat.North, Bid.NoTrumpsBid(1)));
        history.Add(new AuctionBid(Seat.East, Bid.Pass()));
        history.Add(new AuctionBid(Seat.South, Bid.SuitBid(2, Suit.Diamonds))); // transfer, not Stayman
        history.Add(new AuctionBid(Seat.West, Bid.Pass()));

        var handEval = new HandEvaluation
        {
            Hcp = 13, Shape = Shape(3, 4, 3, 3), IsBalanced = true, Losers = 7,
            LongestAndStrongest = Suit.Hearts
        };
        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(Seat.North);
        var ctx = new BiddingContext(new Hand(new List<Card>()), history, Seat.North, Vulnerability.None);
        var decCtx = new DecisionContext(ctx, handEval, aucEval, tableKnowledge);

        Assert.That(GetStaymanResponseRule().CouldMakeBid(decCtx), Is.False);
    }

    #endregion

    // =============================================================
    // RULE FACTORY METHODS
    // Update these once you create the actual rule classes.
    // Expected location: BridgeIt.Core/BiddingEngine/Rules/OpenerRebid/
    // =============================================================

    private static BiddingRuleBase GetStaymanResponseRule()
    {
        return new BridgeIt.Core.BiddingEngine.Conventions.StaymanResponse(
            BridgeIt.Core.BiddingEngine.Conventions.NTConventionContexts.After1NT);
    }

    private static BiddingRuleBase GetNTInviteRule()
        => new AcolOpenerAfterNTInvite();

    private static BiddingRuleBase GetAfterMajorRaiseRule()
        => new AcolOpenerAfterMajorRaise();
}
