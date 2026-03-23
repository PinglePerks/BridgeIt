using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;

public class AcolRedSuitTransferOver2NT: BiddingRuleBase
{
    public override string Name { get; } = "Red Suit Transfer over 2NT";
    public override int Priority { get; } = 30; // Higher priority than a standard suit opening
    private Bid ApplicableOpeningBid => Bid.NoTrumpsBid(2);

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;
        if (auction.BiddingRound != 1) return false;
        if (auction.PartnerLastNonPassBid != ApplicableOpeningBid) return false;
        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Shape[Suit.Hearts] >= 5 || ctx.HandEvaluation.Shape[Suit.Spades] >= 5;

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

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit) return false;
        if (ctx.AuctionEvaluation.OpeningBid != ApplicableOpeningBid) return false;
        if (bid.Level != 3) return false;
        return bid.Suit is Suit.Diamonds or Suit.Hearts;
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