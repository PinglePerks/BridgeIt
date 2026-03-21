using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;

public class AcolRedSuitTransfer: BiddingRuleBase
{
    public override string Name { get; } = "Red Suit Transfer";
    public override int Priority { get; } = 30; // Higher priority than a standard suit opening

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;
        
        if (ctx.AuctionEvaluation.CurrentContract == null) return false;
        
        if (ctx.AuctionEvaluation.CurrentContract.Type != BidType.NoTrumps) return false;

        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        
        return ctx.HandEvaluation.Shape[Suit.Hearts] >= 5 || ctx.HandEvaluation.Shape[Suit.Spades] >= 5;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 5)
        {
            var diamondLevel = GetNextSuitBidLevel(Suit.Diamonds, ctx.AuctionEvaluation.CurrentContract);
            return Bid.SuitBid(diamondLevel, Suit.Diamonds);
        };
        var heartlevel = GetNextSuitBidLevel(Suit.Hearts, ctx.AuctionEvaluation.CurrentContract);
        return Bid.SuitBid(heartlevel, Suit.Hearts);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;
        
        if (ctx.AuctionEvaluation.CurrentContract == null) return false;
        
        if (ctx.AuctionEvaluation.CurrentContract.Type != BidType.NoTrumps) return false;

        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        
        if (bid.Suit != Suit.Diamonds && bid.Suit != Suit.Hearts) return false;
        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        Suit? suit;
        if (bid.Suit == Suit.Diamonds) suit = Suit.Hearts;
        else suit = Suit.Spades;
        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(suit, 5, 11));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}