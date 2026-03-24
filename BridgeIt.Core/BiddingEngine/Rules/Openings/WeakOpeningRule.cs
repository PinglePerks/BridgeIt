using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class WeakOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Weak Opening";
    public override int Priority { get; } = 9;
    private const int Number = 4;
    private const int MinHcp = 6;
    private const int MaxHcp = 9;

    private readonly HashSet<Bid> _reservedBids;
    private readonly List<IBidConstraint> _constraints;

    // Forward: what must be true for any weak opening to fire
    private static CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(MinHcp, MaxHcp), new SuitLengthConstraint("any", ">=6") } };

    // Backward: once we know the specific bid, we know the exact suit and length
    private static CompositeConstraint BuildConstraints(Bid bid)
        => new() { Constraints = { new HcpConstraint(MinHcp, MaxHcp), new SuitLengthConstraint(bid.Suit!.Value, bid.Level + Number, bid.Level + Number) } };

    public WeakOpeningRule(IEnumerable<Bid> reservedBids)
    {
        _constraints = [new HcpConstraint(MinHcp, MaxHcp), new SuitLengthConstraint("any", ">=6")];
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
        return Bid.SuitBid(level - Number, suit);
    }
}