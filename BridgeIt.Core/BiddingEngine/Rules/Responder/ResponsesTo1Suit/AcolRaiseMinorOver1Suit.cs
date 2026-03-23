using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolRaiseMinorOver1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol raise minor over 1 suit";
    public override int Priority { get; } = 35;
    public override CompositeConstraint? GetMinimumForwardRequirements(AuctionEvaluation auction)
    {
        var suit = auction.OpeningBid?.Suit;
        if (suit == null) return null;
        return new CompositeConstraint
        {
            Constraints =
            {
                new HcpConstraint(6, 40),
                new SuitLengthConstraint(suit.Value, 4, 13)
            }
        };
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 1)
            return false;
        if (auction.OpeningBid!.Type != BidType.Suit || auction.OpeningBid.Level != 1)
            return false;
        // Only applies when partner opened a minor
        if (auction.OpeningBid.Suit != Suit.Clubs && auction.OpeningBid.Suit != Suit.Diamonds)
            return false;
        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < 6) return false;

        var openingSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;

        // Must have 4+ card support in partner's minor
        if (ctx.HandEvaluation.Shape[openingSuit] < 4) return false;

        // Should not have a 4-card major available to show at the 1 level
        // (showing a major is more informative than raising a minor)
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4) return false;
        if (ctx.HandEvaluation.Shape[Suit.Spades] >= 4) return false;

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = (Suit)ctx.AuctionEvaluation.OpeningBid!.Suit!;
        var hcp = ctx.HandEvaluation.Hcp;

        if (hcp < 10)
            return Bid.SuitBid(2, suit);
        // 10-12: limit raise
        return Bid.SuitBid(3, suit);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;
        // Must be raising the opening minor suit
        if (bid.Suit != ctx.AuctionEvaluation.OpeningBid!.Suit)
            return false;
        // Valid raise levels: 2 (simple) or 3 (limit)
        if (bid.Level < 2 || bid.Level > 3)
            return false;
        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 10));

        if (bid.Level == 2)
            constraints.Add(new HcpConstraint(6, 9));
        else if (bid.Level == 3)
            constraints.Add(new HcpConstraint(10, 12));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
