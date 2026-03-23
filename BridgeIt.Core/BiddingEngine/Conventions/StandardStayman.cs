using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Standard Stayman convention — asks partner for a 4-card major after an NT bid.
/// Parameterised by NTConventionContext so the same class works after 1NT, 2NT, or 2C-2D-2NT.
///
/// For Puppet Stayman or Checkback Stayman, use a different rule class with the same
/// NTConventionContext infrastructure.
/// </summary>
public class StandardStayman : BiddingRuleBase
{
    private readonly NTConventionContext _ntCtx;

    public StandardStayman(NTConventionContext ntCtx, int priority = 29)
    {
        _ntCtx = ntCtx;
        Priority = priority;
    }

    public override string Name => $"Stayman over {_ntCtx.Name}";
    public override int Priority { get; }

    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(_ntCtx.StaymanHcpMin, 40) } };

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase == AuctionPhase.Uncontested && _ntCtx.ResponderIsTriggered(auction);

    protected override bool IsHandApplicable(DecisionContext ctx)
        => (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4 || ctx.HandEvaluation.Shape[Suit.Spades] >= 4)
           && ctx.HandEvaluation.Hcp >= _ntCtx.StaymanHcpMin;

    public override Bid? Apply(DecisionContext ctx) => _ntCtx.StaymanBid;

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid == _ntCtx.StaymanBid;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new HcpConstraint(_ntCtx.StaymanHcpMin, 30);
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
