using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;

public class AcolStaymanOver1NT: BiddingRuleBase
{
    public override string Name { get; } = "Stayman";
    public override int Priority { get; } = 29; // Higher priority than a standard suit opening
    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(1);
    
    private int HcpMin => 11;

    public override bool CouldMakeBid(DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;
        
        if (ctx.AuctionEvaluation.CurrentContract != ApplicableOpeningBid) return false;
        
        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        
        return (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4 || ctx.HandEvaluation.Shape[Suit.Spades] >= 4) && ctx.HandEvaluation.Hcp >= HcpMin;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.SuitBid(2, Suit.Clubs);
    }

    public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
            return false;
        
        if (ctx.AuctionEvaluation.CurrentContract != ApplicableOpeningBid) return false;
        
        if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
        
        if (bid.Suit != Suit.Clubs || bid.Level != 2) return false;
        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new HcpConstraint(HcpMin, 30);

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}