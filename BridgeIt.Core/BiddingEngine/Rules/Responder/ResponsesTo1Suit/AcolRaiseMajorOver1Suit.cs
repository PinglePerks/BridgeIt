using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolRaiseMajorOver1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol raise major over 1 suit";
    public override int Priority { get; }

    public AcolRaiseMajorOver1Suit(int priority = 50)
    {
        Priority = priority;
    }

    private const int AssumedOpenerLtc = 7;

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
    {
        var suit = auction.OpeningBid?.Suit;
        if (suit == null) return null;
        return new CompositeConstraint
        {
            Constraints =
            {
                new LosingTrickCountConstraint(0, 9),
                new SuitLengthConstraint(suit.Value, 4, 13)
            }
        };
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Responder && auction.BiddingRound == 1)
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                if (auction.OpeningBid.Suit == Suit.Spades || auction.OpeningBid.Suit == Suit.Hearts)
                    return true;
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Losers > 9) return false;

        var openingBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;

        if (ctx.HandEvaluation.Shape[(Suit)openingBidSuit!] >= 4)
        {
            return true;
        }

        return false;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = (Suit)ctx.AuctionEvaluation.OpeningBid!.Suit!;
        var myLosers = ctx.HandEvaluation.Losers;
        var expectedTricks = LosingTrickCount.ExpectedTricks(myLosers, AssumedOpenerLtc);
        var level = Math.Clamp(LosingTrickCount.BidLevel(expectedTricks), 2, 4);
        return Bid.SuitBid(level, suit);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if(bid.Type == BidType.Suit && bid.Level >= 2 && bid.Level <= 4)
            if (bid.Suit == ctx.AuctionEvaluation.OpeningBid!.Suit)
            {
                return true;
            }

        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var suit = bid.Suit;
        var constraints = new CompositeConstraint();
        constraints.Add(new SuitLengthConstraint(suit, 4, 10));
        // LTC that produces this level: level = 24 - (7 + losers) - 6 = 11 - losers
        // So losers = 11 - level
        if (bid.Level == 2)
            constraints.Add(new LosingTrickCountConstraint(9, 9));
        else if (bid.Level == 3)
            constraints.Add(new LosingTrickCountConstraint(8, 8));
        else if (bid.Level == 4)
            constraints.Add(new LosingTrickCountConstraint(0, 7));

        return new BidInformation(bid, constraints, PartnershipBiddingState.FitEstablished);
    }
    
}