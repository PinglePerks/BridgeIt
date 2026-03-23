using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol1NTOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1NT Opening";
    public override int Priority { get; } = 20; // Higher priority than a standard suit opening
    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(MinHcp, 40) } };

    private const int MinHcp = 12;
    private const int MaxHcp = 14;

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
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        constraints.Add(new BalancedConstraint()); // Assuming you have this!
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}