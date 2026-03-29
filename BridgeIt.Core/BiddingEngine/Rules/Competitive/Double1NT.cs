using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

public class Double1NT : BiddingRuleBase
{
    public override string Name { get; } = "Overcall Double 1NT";
    public override int Priority { get; }
    private readonly int _minHcp;
    private readonly int _maxHcp;


    public Double1NT(int minHcp = 16, int maxHcp = 20, int priority = 17)
    {
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        Priority = priority;
    }
    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.CurrentContract == Bid.NoTrumpsBid(1) && auction.SeatRoleType == SeatRoleType.Overcaller;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if(ctx.HandEvaluation.IsBalanced && hcp <= _maxHcp && hcp >= _minHcp) return true;
        return false;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.Double();
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if(bid.Type == BidType.Double) return true;
        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var compositeConstraints = new CompositeConstraint();
        
        compositeConstraints.Add(new HcpConstraint(_minHcp, _maxHcp));
        return new BidInformation(bid, compositeConstraints, PartnershipBiddingState.ConstructiveSearch);
    }
}