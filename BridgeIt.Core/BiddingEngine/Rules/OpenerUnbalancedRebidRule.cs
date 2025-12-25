using System.ComponentModel;
using System.Runtime.InteropServices.JavaScript;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class OpenerUnbalancedRebidRule : BiddingRuleBase
{
    public override string Name { get; } = "Codebased---Opener Unbalanced Rebid";
    public override int Priority { get; } = 25;
    public override bool IsApplicable(DecisionContext ctx)
    {
        var opener = ctx.AuctionEvaluation.SeatRoleType == SeatRoleType.Opener;

        var isFirstBidSuit = ctx.AuctionEvaluation.OpeningBid?.Type == BidType.Suit;
        
        var secondBid = ctx.Data.AuctionHistory.GetAllBidsFromSeat(ctx.Data.Seat).Count == 1;
        
        return opener && secondBid && isFirstBidSuit;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.NoTrumps) return GetConstraintForBidBalanced(bid, ctx);
        if (bid.Type != BidType.Suit) return null;
        
        var compositeConstraint = new CompositeConstraint();
        var seatBids = ctx.Data.AuctionHistory.GetAllBidsFromSeat(ctx.Data.Seat);
        
        var firstSuit = seatBids.First().Suit;
        var secondSuit = bid.Suit;
        
        if (firstSuit == secondSuit)
        {
            compositeConstraint.Constraints.Add(new SuitLengthConstraint(firstSuit!.Value.ToString(), ">=6"));
        }
        else
        {
            compositeConstraint.Constraints.Add(new SuitLengthConstraint(firstSuit!.Value.ToString(), ">=5"));
            compositeConstraint.Constraints.Add(new SuitLengthConstraint(secondSuit!.Value.ToString(), ">=4"));
        }
        
        var previousContract = ctx.AuctionEvaluation.CurrentContract;

        var nextLevel = GetNextSuitBidLevel(bid.Suit!.Value, previousContract);

        if (bid.Level > nextLevel)
        {
            compositeConstraint.Constraints.Add(new HcpConstraint(">=15"));
        }
        return new BidInformation(bid, compositeConstraint, string.Empty);
    }

    public BidInformation? GetConstraintForBidBalanced(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new BalancedConstraint());
        if (bid.Level == 1) constraints.Add(new HcpConstraint("15-17"));
        if (bid.Level == 2) constraints.Add(new HcpConstraint("18-19"));
        return new BidInformation(bid, constraints, string.Empty);
    }

    
    public override Bid? Apply(DecisionContext ctx)
    {
        if(ctx.HandEvaluation.IsBalanced) return ApplyBalancedRebid(ctx);
        var secondSuit = GetSecondSuit(ctx);
        
        if (secondSuit == null) return null;
        
        var biddingDecision = GetBidLevel(ctx, secondSuit.Value);
        
        return biddingDecision;
    }

    protected internal virtual Bid? ApplyBalancedRebid(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp <= 17) return Bid.NoTrumpsBid(1);
        return Bid.NoTrumpsBid(2);
    }
    
    
    
    protected internal virtual Suit? GetSecondSuit(DecisionContext ctx)
    {
        var firstBidSuit = ctx.Data.AuctionHistory.GetAllBidsFromSeat(ctx.Data.Seat).First().Suit;
        var shape = ctx.HandEvaluation.Shape;
        
        if (shape[firstBidSuit!.Value] >= 6)
        {
            return firstBidSuit.Value;
        }

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            if (suit == firstBidSuit) continue;

            if (shape[suit] >= 4)
            {
                return suit;
            }
        }

        return null;
    }
    
    protected internal virtual Bid? GetBidLevel(DecisionContext ctx, Suit bidSuit)
    {
        var lowestBidLevel = GetNextSuitBidLevel(bidSuit, ctx.AuctionEvaluation.CurrentContract);
        
        var bidLevel = lowestBidLevel;

        if (lowestBidLevel < 3)
        {
            bidLevel += JumpBid(ctx);
        }
        
        return Bid.SuitBid(bidLevel, bidSuit);
        
    }


    protected internal virtual int JumpBid(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;

        if (hcp >= 15)
        {
            return 1;
        }

        return 0;
    }
    
    
}