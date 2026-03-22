using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;

/// <summary>
/// Handles responder's balanced NT raises after partner opens 1NT (12-14):
///   Pass  = 0-10 HCP (combined max 24, can't make game)
///   2NT   = 11-12 HCP (invitational — partner bids 3NT with 13-14, passes with 12)
///   3NT   = 13+ HCP  (combined min 25, game is certain)
///
/// Priority 15: lower than transfers (30) and Stayman (29).
/// Hands with 5+ majors are handled by transfers; hands with 4-card major + 11+ by Stayman.
/// This rule catches everything that falls through.
/// </summary>
public class AcolNT11PointRaiseOver1NT : BiddingRuleBase
{
    public override string Name { get; } = "NT 11 Point Raise over 1NT";
    public override int Priority { get; } = 16;

    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(1);

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.AuctionPhase != AuctionPhase.Uncontested) return false;
        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid != ApplicableOpeningBid) return false;
        if (ctx.HandEvaluation.Hcp != 11) return false;

        // This rule always applies for responder's first bid after 1NT.
        // Higher-priority rules (transfers, Stayman) will have already
        // claimed hands with 5-card majors or 4-card major + 11+ HCP.
        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.SuitBid(2, Suit.Spades);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.AuctionPhase != AuctionPhase.Uncontested) return false;
        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid != ApplicableOpeningBid) return false;

        // Explains Pass, 2NT, or 3NT after 1NT opening
        if (bid.Type == BidType.Suit && bid.Level == 2 && bid.Suit == Suit.Spades) return false;

        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {


        return new BidInformation(bid, new HcpConstraint(11,11), PartnershipBiddingState.ConstructiveSearch);
    }
}
