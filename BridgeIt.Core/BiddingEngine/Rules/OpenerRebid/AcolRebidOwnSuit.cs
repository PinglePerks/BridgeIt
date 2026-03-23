using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidOwnSuit : BiddingRuleBase
{
    public override string Name { get; } = "Acol rebid own suit";
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
        
        var shape = ctx.HandEvaluation.Shape;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;
        
        var lengthOfFirstSuit = shape[firstBidSuit!.Value];

        if (lengthOfFirstSuit >= 6)
            return true;

        return false;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;

        var suit = ctx.AuctionEvaluation.OpeningBid!.Suit;

        if (suit == null)
            return null;

        suit = (Suit)suit;
        
        var nextLevel = GetNextSuitBidLevel((Suit)suit, ctx.AuctionEvaluation.CurrentContract);

        if (nextLevel <= 2)
        {
            if (hcp >= 16)
            {
                return Bid.SuitBid(nextLevel + 1, (Suit)suit);
            }
        }
        return Bid.SuitBid(nextLevel, (Suit)suit);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        var openingBid = ctx.AuctionEvaluation.OpeningBid!;
        
        if (openingBid.Suit != bid.Suit || ctx.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
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
        constraints.Add(new SuitLengthConstraint(ctx.AuctionEvaluation.OpeningBid!.Suit, 6, 10));

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