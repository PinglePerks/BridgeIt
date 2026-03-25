using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Responder's rebid after a Stayman sequence (e.g. 1NT-2C-2D/2H/2S).
/// Places the contract based on whether a major fit was found and combined strength.
///
/// With fit (4+4 in a major): raise to game (4M) or invite (3M).
/// Without fit (2D denial or mismatched major): bid 3NT (game) or 2NT (invite).
///
/// Parameterised by NTConventionContext so the same class works after 1NT, 2NT, or 2C-2D-2NT.
/// </summary>
public class AcolResponderAfterStayman : BiddingRuleBase
{
    private readonly NTConventionContext _ntCtx;

    public AcolResponderAfterStayman(NTConventionContext ntCtx, int priority = 55)
    {
        _ntCtx = ntCtx;
        Priority = priority;
    }

    public override string Name => $"Responder after Stayman ({_ntCtx.Name})";
    public override int Priority { get; }
    public override bool IsAlertable => false;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder) return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;

        // I must have bid Stayman last round
        if (auction.MyLastNonPassBid != _ntCtx.StaymanBid) return false;

        // Partner must have responded to Stayman (suit bid at convention level)
        var partnerBid = auction.PartnerLastNonPassBid;
        if (partnerBid == null || partnerBid.Type != BidType.Suit) return false;
        if (partnerBid.Level != _ntCtx.ConventionLevel) return false;

        // Partner's response should be D, H, or S (not C — that's the Stayman bid itself)
        if (partnerBid.Suit == Suit.Clubs) return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastNonPassBid!;
        var fitSuit = FindFitSuit(partnerBid, ctx);
        var verdict = ctx.GetLevelVerdict(25);

        if (fitSuit != null)
        {
            // Major fit found — raise to game or invite
            return verdict == LevelVerdict.BidGame
                ? Bid.SuitBid(4, fitSuit.Value)
                : Bid.SuitBid(3, fitSuit.Value);
        }

        // No fit — bid NT
        return verdict == LevelVerdict.BidGame
            ? Bid.NoTrumpsBid(3)
            : Bid.NoTrumpsBid(2);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        // Bids this rule can explain: 4H, 4S, 3H, 3S, 3NT, 2NT
        if (bid.Type == BidType.NoTrumps && (bid.Level == 2 || bid.Level == 3)) return true;
        if (bid.Type == BidType.Suit && bid.Suit is Suit.Hearts or Suit.Spades && (bid.Level == 3 || bid.Level == 4)) return true;
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(_ntCtx.StaymanHcpMin, 30));

        if (bid.Type == BidType.Suit && bid.Suit.HasValue)
            constraints.Add(new SuitLengthConstraint(bid.Suit.Value, 4, 13));

        var state = bid.Level >= 4 || (bid.Type == BidType.NoTrumps && bid.Level == 3)
            ? PartnershipBiddingState.SignOff
            : PartnershipBiddingState.GameInvitational;

        return new BidInformation(bid, constraints, state);
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(_ntCtx.StaymanHcpMin, 30) } };

    private static Suit? FindFitSuit(Bid partnerBid, DecisionContext ctx)
    {
        if (partnerBid.Suit == Suit.Hearts && ctx.HandEvaluation.Shape[Suit.Hearts] >= 4)
            return Suit.Hearts;

        if (partnerBid.Suit == Suit.Spades && ctx.HandEvaluation.Shape[Suit.Spades] >= 4)
            return Suit.Spades;

        return null;
    }
}
