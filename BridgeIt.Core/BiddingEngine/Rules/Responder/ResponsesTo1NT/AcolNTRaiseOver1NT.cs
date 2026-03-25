using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

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
public class AcolNTRaiseOver1NT : BiddingRuleBase
{
    public override string Name { get; } = "NT Raise over 1NT";
    public override int Priority { get; } = 15;

    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(1);

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;
        if (auction.BiddingRound != 1) return false;
        if (auction.PartnerLastNonPassBid != ApplicableOpeningBid) return false;
        return true;
    }

    // No IsHandApplicable override — this is the catch-all after transfers/Stayman

    public override Bid? Apply(DecisionContext ctx)
    {
        return ctx.GetLevelVerdict() switch
        {
            LevelVerdict.SignOff => Bid.Pass(),
            LevelVerdict.Invite => Bid.NoTrumpsBid(2),
            LevelVerdict.BidGame => Bid.NoTrumpsBid(3),
            _ => Bid.Pass()
        };
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        // Explains Pass, 2NT, or 3NT after 1NT opening
        if (bid.Type == BidType.Pass) return true;
        if (bid.Type == BidType.NoTrumps && bid.Level is 2 or 3) return true;
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Pass)
        {
            // Pass = 0-10 HCP, no 5-card major (transfers would have fired)
            var constraints = new HcpConstraint(0, 10);
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        if (bid.Type == BidType.NoTrumps && bid.Level == 2)
        {
            // 2NT = 12 HCP invitational
            var constraints = new HcpConstraint(12, 12);
            return new BidInformation(bid, constraints, PartnershipBiddingState.GameInvitational);
        }

        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
        {
            // 3NT = 13+ game-forcing
            var constraints = new HcpConstraint(13, 30);
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        return null;
    }
}
