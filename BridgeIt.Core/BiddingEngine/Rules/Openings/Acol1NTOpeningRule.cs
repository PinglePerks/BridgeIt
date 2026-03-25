using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol1NTOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT Opening";
    public override int Priority { get; } = 20;

    private const int MinHcp = 12;
    private const int MaxHcp = 14;

    private static CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(MinHcp, MaxHcp), new BalancedConstraint() } };

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp
           && ctx.HandEvaluation.IsBalanced;

    public override Bid? Apply(DecisionContext ctx)
        => Bid.NoTrumpsBid(1);

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid is { Type: BidType.NoTrumps, Level: 1 };

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, BuildConstraints(), PartnershipBiddingState.ConstructiveSearch);
}