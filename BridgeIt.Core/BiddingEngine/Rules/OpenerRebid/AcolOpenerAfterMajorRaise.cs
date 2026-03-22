using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolOpenerAfterMajorRaise : BiddingRuleBase
{
    public override string Name { get; } = "After major raise";
    public override int Priority { get; } = 45;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;

        var partnerLastBid = auction.PartnerLastBid;
        
        if (partnerLastBid == null) return false;
        
        if (partnerLastBid.Type != BidType.Suit) return false;
        
        if (auction.OpeningBid!.Suit == partnerLastBid.Suit)
            return true;

        if (partnerLastBid is { Type: BidType.NoTrumps, Level: 2 })
            return true;

        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        return true;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        var level = ctx.GetLevelVerdict();
        var suit = (Suit)ctx.AuctionEvaluation.OpeningBid!.Suit!;
        
        return level switch
        {
            LevelVerdict.BidGame => Bid.SuitBid(4, suit),
            LevelVerdict.Invite => Bid.SuitBid(3, suit),
            LevelVerdict.SignOff => Bid.Pass(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        return true;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
    }
}