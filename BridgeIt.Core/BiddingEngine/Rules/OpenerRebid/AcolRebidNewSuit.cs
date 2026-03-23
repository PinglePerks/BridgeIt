using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidNewSuit : BiddingRuleBase
{
    public override string Name { get; } = "Acol rebid new suit";
    public override int Priority { get; } = 40;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 2)
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
            return false;

        if (ctx.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;

        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid!;

        var suits = ctx.HandEvaluation.SuitsByLengthDescending();
        
        var shape = ctx.HandEvaluation.Shape;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;
        
        var lengthOfFirstSuit = shape[firstBidSuit!.Value];

        if (lengthOfFirstSuit >= 6)
            return false;
        
        var secondSuit = suits[1];

        if (secondSuit == partnerBid.Suit)
            return false;

        return true;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        
        var secondSuit = ctx.HandEvaluation.SuitsByLengthDescending()[1];
        
        
        
        var nextLevel = GetNextSuitBidLevel(secondSuit, ctx.AuctionEvaluation.CurrentContract);

        if (nextLevel <= 2)
        {
            if (hcp >= 16)
            {
                return Bid.SuitBid(nextLevel + 1, secondSuit);
            }
        }
        return Bid.SuitBid(nextLevel, secondSuit);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        var openingBid = ctx.AuctionEvaluation.OpeningBid!;
        
        if (openingBid.Suit == bid.Suit || ctx.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;

        return true;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var suit = bid.Suit;
        var level = bid.Level;
        
        if(bid.Suit == null)
            return null;

        var nextLevel = GetNextSuitBidLevel((Suit)bid.Suit, ctx.AuctionEvaluation.CurrentContract);

        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(ctx.AuctionEvaluation.OpeningBid!.Suit, 5, 5));
        constraints.Add(new SuitLengthConstraint(suit, 4, 5));

        if (nextLevel == 3)
        {
            constraints.Add(new HcpConstraint(13, 19));
        }
        else
        {
            if (level == nextLevel)
            {
                constraints.Add(new HcpConstraint(12, 15));
            }
            else
            {
                constraints.Add(new HcpConstraint(16, 19));
            }
        }
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}