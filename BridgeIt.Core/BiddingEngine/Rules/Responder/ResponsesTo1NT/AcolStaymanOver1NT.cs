using BridgeIt.Core.Analysis.Auction;
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

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;
        if (auction.BiddingRound != 1) return false;
        if (auction.PartnerLastNonPassBid != ApplicableOpeningBid) return false;
        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
        => (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4 || ctx.HandEvaluation.Shape[Suit.Spades] >= 4)
           && ctx.HandEvaluation.Hcp >= HcpMin;

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.SuitBid(2, Suit.Clubs);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Suit == Suit.Clubs && bid.Level == 2;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new HcpConstraint(HcpMin, 30);

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}