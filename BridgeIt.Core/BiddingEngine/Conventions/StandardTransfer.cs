using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Red suit (Jacoby) transfer convention — bid diamonds to show hearts, bid hearts to show spades.
/// Parameterised by NTConventionContext so the same class works after 1NT, 2NT, or 2C-2D-2NT.
/// </summary>
public class StandardTransfer : BiddingRuleBase
{
    private readonly NTConventionContext _ntCtx;

    public StandardTransfer(NTConventionContext ntCtx, int priority = 30)
    {
        _ntCtx = ntCtx;
        Priority = priority;
    }

    public override string Name => $"Red Suit Transfer over {_ntCtx.Name}";
    public override int Priority { get; }
    public override bool IsAlertable => true;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase == AuctionPhase.Uncontested && _ntCtx.ResponderIsTriggered(auction);

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Shape[Suit.Hearts] >= 5 || ctx.HandEvaluation.Shape[Suit.Spades] >= 5;

    public override Bid? Apply(DecisionContext ctx)
    {
        // Transfer one suit below: diamonds → hearts, hearts → spades
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 5)
            return _ntCtx.HeartTransferBid;
        return _ntCtx.SpadeTransferBid;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid == _ntCtx.HeartTransferBid || bid == _ntCtx.SpadeTransferBid;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var targetSuit = bid == _ntCtx.HeartTransferBid ? Suit.Hearts : Suit.Spades;
        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(targetSuit, 5, 11));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
