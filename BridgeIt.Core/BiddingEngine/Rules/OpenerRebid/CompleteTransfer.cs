using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class CompleteTransfer : BiddingRuleBase
{
    public override string Name { get; } = "Complete transfer";
    public override int Priority { get; } = 30; // Higher priority than a standard suit opening
    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(1);

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.AuctionPhase != AuctionPhase.Uncontested || auction.SeatRoleType != SeatRoleType.Opener) return false;
        if (auction.BiddingRound != 2) return false;
        if (auction.MyLastNonPassBid != ApplicableOpeningBid) return false;

        // Partner's last bid was the transfer (2D or 2H)
        return auction.PartnerLastBid == Bid.SuitBid(2, Suit.Diamonds) ||
               auction.PartnerLastBid == Bid.SuitBid(2, Suit.Hearts);
    }

    // No IsHandApplicable override — transfer completion is compulsory

    public override Bid? Apply(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Diamonds))
            return Bid.SuitBid(2, Suit.Hearts);
        return Bid.SuitBid(2, Suit.Spades);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        // 2D transfer by partner → must be explaining 2H completion
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == Bid.SuitBid(2, Suit.Diamonds))
            return bid == Bid.SuitBid(2, Suit.Hearts);

        // 2H transfer by partner → must be explaining 2S completion
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == Bid.SuitBid(2, Suit.Hearts))
            return bid == Bid.SuitBid(2, Suit.Spades);

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        return new BidInformation(bid, null, PartnershipBiddingState.ConstructiveSearch);
    }
}