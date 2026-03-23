using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Opener completes partner's transfer — compulsory bid accepting the transfer suit.
/// Parameterised by NTConventionContext so the same class works after 1NT, 2NT, or 2C-2D-2NT.
/// </summary>
public class CompleteTransfer : BiddingRuleBase
{
    private readonly NTConventionContext _ntCtx;

    public CompleteTransfer(NTConventionContext ntCtx, int priority = 30)
    {
        _ntCtx = ntCtx;
        Priority = priority;
    }

    public override string Name => $"Complete transfer after {_ntCtx.Name}";
    public override int Priority { get; }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener) return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;

        // My last bid was the NT-showing bid
        if (auction.MyLastNonPassBid != Bid.NoTrumpsBid(_ntCtx.NTLevel)) return false;

        // Partner's last bid was one of the transfer bids
        return auction.PartnerLastBid == _ntCtx.HeartTransferBid
            || auction.PartnerLastBid == _ntCtx.SpadeTransferBid;
    }

    // No IsHandApplicable override — transfer completion is compulsory

    public override Bid? Apply(DecisionContext ctx)
    {
        // Step up from transfer suit to target suit
        if (ctx.AuctionEvaluation.PartnerLastBid == _ntCtx.HeartTransferBid)
            return Bid.SuitBid(_ntCtx.ConventionLevel, Suit.Hearts);
        return Bid.SuitBid(_ntCtx.ConventionLevel, Suit.Spades);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == _ntCtx.HeartTransferBid)
            return bid == Bid.SuitBid(_ntCtx.ConventionLevel, Suit.Hearts);
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == _ntCtx.SpadeTransferBid)
            return bid == Bid.SuitBid(_ntCtx.ConventionLevel, Suit.Spades);
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.ConstructiveSearch);
}
