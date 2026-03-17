using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class WeakOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Weak Opening";
    public override int Priority { get; } = 40;
    private int _number = 4;
    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract == null)
        {
            if(ctx.HandEvaluation.Hcp < 10 && ctx.HandEvaluation.Hcp > 5)
            {
                if (ctx.HandEvaluation.Shape.Values.Any(s => s >= 6))
                    return true;
            }
        }
        return false;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = LongestAndStrongest(ctx.Data.Hand);
        var level = ctx.Data.Hand.CountSuit(suit);
        
        return Bid.SuitBid(level - _number, suit);

    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract != null)
        {
            return false;
        }

        if (bid.Type == BidType.Suit && bid.Level >= 2)
        {
            return true;
        } ;
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var bidLevel = bid.Level;
        var bidSuit = bid.Suit;
        
        var levelString = (bidLevel - _number).ToString();
        
        var lengthConstraint = new SuitLengthConstraint(bidSuit.ToString()!, $"={levelString}");
        
        var compositeConstraint = new CompositeConstraint();

        var strengthConstraing = new HcpConstraint("6-10");
        
        compositeConstraint.Constraints.Add(lengthConstraint);
        compositeConstraint.Constraints.Add(strengthConstraing);
        
        
        
        var bidInformation = new BidInformation(bid, compositeConstraint, null);
        
        return bidInformation;
    }
}