using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidBalanced : BiddingRuleBase
{
    public override string Name { get; } = "Acol balanced rebid";
    public override int Priority { get; } = 25;
    private int MinHcp1NTRebid { get; } = 15;
    private int MaxHcp1NTRebid { get; } = 17;
    private int MinHcp2NTRebid { get; } = 18;
    private int MaxHcp2NTRebid { get; } = 19;
    
    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 1)
        {
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
            {
                return true;
            }
        }

        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.IsBalanced)
        {
            return true;
        }

        return false;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp > MinHcp2NTRebid)
        {
            return Bid.NoTrumpsBid(2);
        }

        if (ctx.HandEvaluation.Hcp > MinHcp1NTRebid)
        {
            return Bid.NoTrumpsBid(1);
        }

        return null;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new BalancedConstraint());
        
        if (bid.Type == BidType.NoTrumps && bid.Level == 1)
        {
            constraints.Add(new HcpConstraint(MinHcp1NTRebid, MaxHcp1NTRebid));
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }
        if (bid.Type == BidType.NoTrumps && bid.Level == 2)
        {
            constraints.Add(new HcpConstraint(MinHcp2NTRebid, MaxHcp2NTRebid));
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }
        return null;
    }


}