using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class CompleteTransfer : BiddingRuleBase
{
    public override string Name { get; } = "Complete transfer";
    public override int Priority { get; } = 30; // Higher priority than a standard suit opening
    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(1);

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;

        if (ctx.AuctionEvaluation.OpeningBid != ApplicableOpeningBid) return false;

        if (ctx.AuctionEvaluation.BiddingRound != 2) return false;

        return ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Diamonds) ||
               ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Hearts);
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Diamonds))
            return Bid.SuitBid(2, Suit.Hearts);
        return Bid.SuitBid(2, Suit.Spades);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;

        if (ctx.AuctionEvaluation.OpeningBid != ApplicableOpeningBid) return false;

        if (ctx.AuctionEvaluation.BiddingRound != 2) return false;

        return ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Diamonds) ||
               ctx.AuctionEvaluation.CurrentContract == Bid.SuitBid(2, Suit.Hearts);
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        return new BidInformation(bid, null, PartnershipBiddingState.ConstructiveSearch);
    }
}