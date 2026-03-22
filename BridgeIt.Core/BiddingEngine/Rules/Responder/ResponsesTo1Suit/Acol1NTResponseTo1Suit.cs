using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class Acol1NTResponseTo1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT response to 1 suit";
    public override int Priority { get; } = 30;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Responder && auction.BiddingRound == 1)
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                return true;
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if(ctx.HandEvaluation.Hcp < 6) return false;
        
        return true;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(1);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        return bid.Type == BidType.NoTrumps && bid.Level == 1;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        
        constraints.Add(new HcpConstraint(6, 9));
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}