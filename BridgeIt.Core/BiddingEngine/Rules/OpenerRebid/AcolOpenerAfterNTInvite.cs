using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolOpenerAfterNTInvite : BiddingRuleBase
{
    public override string Name { get; } = "Acol opener after NT invite";
    public override int Priority { get; } = 50;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;
        
        if (auction.OpeningBid == Bid.NoTrumpsBid(1) && auction.PartnerLastNonPassBid == Bid.NoTrumpsBid(2))
            return true;

        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        return true;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        return ctx.GetLevelVerdict() switch
        {
            LevelVerdict.BidGame => Bid.NoTrumpsBid(3),
            _ => Bid.Pass()
        };
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if(bid.Type == BidType.NoTrumps && bid.Level == 3)
            return true;
        if (bid.Type == BidType.Pass)
            return true;
        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            constraints.Add(new HcpConstraint(13, 14));
        if (bid.Type == BidType.Pass)
            constraints.Add(new HcpConstraint(12, 12));
        return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
    }
}