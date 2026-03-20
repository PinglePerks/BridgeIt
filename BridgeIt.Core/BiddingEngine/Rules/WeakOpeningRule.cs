using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules;

public class WeakOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Weak Opening";
    public override int Priority { get; } = 9;
    private const int Number = 4;
    private const int MinHcp = 6;
    private const int MaxHcp = 9;
    
    private List<Bid> _forbiddenBids = [Bid.SuitBid(2, Suit.Clubs)];
    private List<IBidConstraint> _constraints;

    public WeakOpeningRule()
    {
        _constraints = new List<IBidConstraint>();
        _constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        _constraints.Add(new SuitLengthConstraint("any", $">=6"));
        
    }
    public override bool CouldMakeBid(DecisionContext ctx)
    {
        // 1. History Check: Must be the opening bid (no previous bids by anyone)
        if (ctx.Data.AuctionHistory.Bids.Any(b => b.Bid.Type != BidType.Pass))
            return false;

        if (!_constraints.All(c => c.IsMet(ctx)))
            return false;
        
        var bid = MakeBid(ctx);
                
        if (_forbiddenBids.Any(x => bid == x))
            return false;

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return MakeBid(ctx);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract != null)
        {
            return false;
        }

        if (bid.Type == BidType.Suit && bid.Level >= 2 && _forbiddenBids.All(x => x != bid))
        {
            return true;
        }
        
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var bidLevel = bid.Level;
        var bidSuit = bid.Suit;
        
        var levelString = (bidLevel + Number).ToString();
        
        var lengthConstraint = new SuitLengthConstraint(bidSuit.ToString()!, $"={levelString}");
        
        var compositeConstraint = new CompositeConstraint();

        var strengthConstraing = new HcpConstraint(MinHcp, MaxHcp);
        
        compositeConstraint.Constraints.Add(lengthConstraint);
        compositeConstraint.Constraints.Add(strengthConstraing);
        
        
        
        var bidInformation = new BidInformation(bid, compositeConstraint, null);
        
        return bidInformation;
    }

    protected internal virtual Bid MakeBid(DecisionContext ctx)
    {
        var suit = LongestAndStrongest(ctx.Data.Hand);
        
        var level = ctx.Data.Hand.CountSuit(suit);
        
        return Bid.SuitBid(level - Number, suit);
    }
}