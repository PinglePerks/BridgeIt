using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class AcolStrongOpening : BiddingRuleBase
{
    public override string Name { get; } = "Acol Strong Opening";
    public override int Priority { get; } = 19; // Higher priority than a standard suit opening
    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(MinHcp, 40) } };

    private const int MinHcp = 20;
    private const int MaxHcp = 35;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp
           && ctx.HandEvaluation.IsBalanced;

    public override Bid? Apply(DecisionContext ctx)
        => Bid.SuitBid(2, Suit.Clubs);

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid is { Type: BidType.Suit, Level: 2, Suit: Suit.Clubs };

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}