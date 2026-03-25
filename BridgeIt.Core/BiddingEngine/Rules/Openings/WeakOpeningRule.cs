using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class WeakOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Weak Opening";
    public override int Priority { get; }
    private readonly int _number;
    private readonly int _minHcp;
    private readonly int _maxHcp;
    private readonly int _minSuitLength;

    private readonly HashSet<Bid> _reservedBids;
    private readonly List<IBidConstraint> _constraints;

    // Forward: what must be true for any weak opening to fire
    private CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint("any", $">={_minSuitLength}") } };

    // Backward: once we know the specific bid, we know the exact suit and length
    private CompositeConstraint BuildConstraints(Bid bid)
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint(bid.Suit!.Value, bid.Level + _number, bid.Level + _number) } };

    public WeakOpeningRule(IEnumerable<Bid> reservedBids, int minHcp = 6, int maxHcp = 9,
        int minSuitLength = 6, int number = 4, int priority = 9)
    {
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        _minSuitLength = minSuitLength;
        _number = number;
        Priority = priority;
        _constraints = [new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint("any", $">={_minSuitLength}")];
        _reservedBids = reservedBids.ToHashSet();
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (!_constraints.All(c => c.IsMet(ctx)))
            return false;

        var bid = MakeBid(ctx);
        return !_reservedBids.Contains(bid);
    }

    public override Bid? Apply(DecisionContext ctx)
        => MakeBid(ctx);

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit && bid.Level >= 2 && !_reservedBids.Contains(bid);

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, BuildConstraints(bid), PartnershipBiddingState.ConstructiveSearch);

    protected internal virtual Bid MakeBid(DecisionContext ctx)
    {
        var suit = ctx.HandEvaluation.LongestAndStrongest;
        var level = ctx.HandEvaluation.Shape[suit];
        return Bid.SuitBid(level - _number, suit);
    }
}