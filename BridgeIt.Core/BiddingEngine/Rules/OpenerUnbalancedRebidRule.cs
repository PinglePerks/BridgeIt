using System.ComponentModel;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class OpenerUnbalancedRebidRule : BiddingRuleBase
{
    public override int Priority { get; } = 25;
    public override bool IsApplicable(BiddingContext ctx)
    {
        var opener = ctx.AuctionEvaluation.SeatRole == SeatRole.Opener;
        var secondBid = ctx.AuctionHistory.GetAllSeatBids(ctx.Seat).Count() == 1;
        var unbalanced = !ctx.HandEvaluation.IsBalanced;
        return opener && secondBid && unbalanced;

    }

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        var compositeConstraint = new CompositeConstraint();

        var firstBid = ctx.AuctionHistory.GetAllSeatBids(ctx.Seat).First().ChosenBid;
        var currentContract = ctx.AuctionEvaluation.CurrentContract;

        if (ctx.HandEvaluation.IsBalanced) return null;
        
        var secondSuit = GetSecondSuit(ctx, compositeConstraint);
        
        if (secondSuit == null) return null;
        
        
        var biddingDecision = GetBidLevel(ctx, secondSuit.Value, compositeConstraint);
        
        if (biddingDecision == null) return null;
        
        return biddingDecision;
    }

    protected internal virtual Suit? GetSecondSuit(BiddingContext ctx, CompositeConstraint compositeConstraint)
    {
        var firstBidSuit = ctx.AuctionHistory.GetAllSeatBids(ctx.Seat).First().ChosenBid.Suit;
        var shape = ctx.HandEvaluation.Shape;
        if (firstBidSuit == null) return Suit.Clubs;
        if (shape[firstBidSuit!.Value] >= 6)
        {
            compositeConstraint.Add(new SuitLengthConstraint(firstBidSuit.Value.ToString(), ">=6"));
            return firstBidSuit.Value;
        }
        compositeConstraint.Add(new SuitLengthConstraint(firstBidSuit.Value.ToString(), ">=5"));

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            if (suit == firstBidSuit) continue;

            if (shape[suit] >= 4)
            {
                compositeConstraint.Add(new SuitLengthConstraint(suit.ToString(), ">=4"));
                return suit;
            }
        }

        return null;

    }
    
    protected internal virtual BiddingDecision? GetBidLevel(BiddingContext ctx, Suit bidSuit, CompositeConstraint compositeConstraint)
    {
        var lowestBidLevel = GetNextSuitBidLevel(bidSuit, ctx.AuctionEvaluation.CurrentContract);
        
        var hcp = ctx.HandEvaluation.Hcp;
        
        if (hcp >= 15 && lowestBidLevel == 1)
        {
            compositeConstraint.Add(new HcpConstraint(">=15"));
            return new BiddingDecision(
                Bid.SuitBid(lowestBidLevel + 1, bidSuit),
                "strong - over 15 hcp",
                "natural",
                compositeConstraint);
        }
        
        return new BiddingDecision(
            Bid.SuitBid(lowestBidLevel, bidSuit),
            "second suit",
            "natural",
            compositeConstraint);
    }
}