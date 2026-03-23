using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolJacoby2NTOver1Major :  BiddingRuleBase
{
    public override string Name { get; } = "Acol Jacoby 2NT over 1 major";
    public override int Priority { get; } = 55;
    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
    {
        var suit = auction.OpeningBid?.Suit;
        if (suit == null) return null;
        return new CompositeConstraint
        {
            Constraints =
            {
                new HcpConstraint(13, 40),
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
        if (ctx.HandEvaluation.Hcp >= 13)
        {
            var openingBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;
            if (ctx.HandEvaluation.Shape[(Suit)openingBidSuit!] >= 4)
            {
                return true;
            }
        }
        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        
        constraints.Add(new HcpConstraint(13,30));
        
        constraints.Add(new SuitLengthConstraint(ctx.AuctionEvaluation.OpeningBid!.Suit, 4, 10));
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.FitEstablished);
    }
    
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        return bid.Type == BidType.NoTrumps && bid.Level == 2;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(2);
    }
}