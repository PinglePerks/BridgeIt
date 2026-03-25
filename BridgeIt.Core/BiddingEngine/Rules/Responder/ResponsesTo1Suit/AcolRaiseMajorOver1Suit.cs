using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolRaiseMajorOver1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol raise major over 1 suit";
    public override int Priority { get; } = 50;
    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
    {
        var suit = auction.OpeningBid?.Suit;
        if (suit == null) return null;
        return new CompositeConstraint
        {
            Constraints =
            {
                new HcpConstraint(6, 40),
                new SuitLengthConstraint(suit.Value, 4, 13)
            }
        };
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Responder && auction.BiddingRound == 1)
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                if (auction.OpeningBid.Suit == Suit.Spades || auction.OpeningBid.Suit == Suit.Hearts)
                    return true;
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < 6) return false;

        var openingBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;
        
        if (ctx.HandEvaluation.Shape[(Suit)openingBidSuit!] >= 4)
        {
            return true;
        }
        
        return false;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = (Suit)ctx.AuctionEvaluation.OpeningBid!.Suit!;
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp < 10)
            return Bid.SuitBid(2, suit);
        if (hcp < 13)
            return Bid.SuitBid(3, suit);
        // 13+ — game raise (safety net if Jacoby 2NT didn't fire)
        return Bid.SuitBid(4, suit);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if(bid.Type == BidType.Suit && bid.Level >= 2 && bid.Level <= 4)
            if (bid.Suit == ctx.AuctionEvaluation.OpeningBid!.Suit)
            {
                return true;
            }

        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var suit = bid.Suit;
        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(suit, 4, 10));
        if(bid.Level == 2)
            constraints.Add(new HcpConstraint(6,9));
        if(bid.Level == 3)
            constraints.Add(new HcpConstraint(10, 12));
        if(bid.Level == 4)
            constraints.Add(new HcpConstraint(13, 30));
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.FitEstablished);
    }
    
}