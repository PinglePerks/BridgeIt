using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolStaymanResponse : BiddingRuleBase
{
    public override string Name { get; } = "Acol stayman response";
    public override int Priority { get; } = 60;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;
        
        if (auction.OpeningBid == Bid.NoTrumpsBid(1) && auction.PartnerLastBid == Bid.SuitBid(2, Suit.Clubs) && auction.CurrentContract == Bid.SuitBid(2,Suit.Clubs))
            return true;

        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
        => true;
    public override Bid? Apply(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4)
            return Bid.SuitBid(2, Suit.Hearts);
        
        if (ctx.HandEvaluation.Shape[Suit.Spades] >= 4)
            return Bid.SuitBid(2, Suit.Spades);
        
        return Bid.SuitBid(2, Suit.Diamonds);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.Suit && bid.Level == 2 && bid.Suit != Suit.Clubs)
            return true;
        
        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        if(bid.Suit == Suit.Hearts)
            constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 5));

        if (bid.Suit == Suit.Spades)
        {
            constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 5));
            constraints.Add(new SuitLengthConstraint(Suit.Hearts, 2, 3));
        }

        if (bid.Suit == Suit.Diamonds)
        {
            constraints.Add(new SuitLengthConstraint(Suit.Hearts, 2, 3));
            constraints.Add(new SuitLengthConstraint(Suit.Spades, 2, 3));
        }
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}