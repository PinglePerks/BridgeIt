using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol1NTOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT Opening";
    public override int Priority { get; }

    private readonly int _minHcp;
    private readonly int _maxHcp;
    private readonly int _level;

    public Acol1NTOpeningRule(int minHcp = 12, int maxHcp = 14, int level = 1, int priority = 20)
    {
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        _level = level;
        Priority = priority;
    }

    private CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new BalancedConstraint() } };

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp >= _minHcp && ctx.HandEvaluation.Hcp <= _maxHcp
           && ctx.HandEvaluation.IsBalanced;

    public override Bid? Apply(DecisionContext ctx)
        => Bid.NoTrumpsBid(_level);

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.NoTrumps && bid.Level == _level;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, BuildConstraints(), PartnershipBiddingState.ConstructiveSearch);
}