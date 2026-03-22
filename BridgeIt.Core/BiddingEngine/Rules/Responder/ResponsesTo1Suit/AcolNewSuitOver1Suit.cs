using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1Suit;

public class AcolNewSuitOver1Suit : BiddingRuleBase
{
    public override string Name { get; } = "Acol new suit over 1 suit";
    public override int Priority { get; } = 40;


    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Responder && auction.BiddingRound == 1)
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                return true;
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < 6) return false;
        return true;
    }
    public override Bid? Apply(DecisionContext ctx)
    {
        var suitToBid = ctx.HandEvaluation.LongestAndStrongest;
        
        var hcp = ctx.HandEvaluation.Hcp;
        
        var levelNeeded = GetNextSuitBidLevel(suitToBid, ctx.AuctionEvaluation.CurrentContract);
        
        //if bidding major at 2 level, need 5 in suit
        if (levelNeeded == 2 && hcp >= 10)
        {
            if (suitToBid == Suit.Hearts || suitToBid == Suit.Spades)
            {
                if (ctx.HandEvaluation.Shape[suitToBid] >= 5)
                {
                    return Bid.SuitBid(2, suitToBid);
                }
                var minorSuit = ctx.HandEvaluation.Shape[Suit.Clubs] >= ctx.HandEvaluation.Shape[Suit.Diamonds] ? Suit.Clubs : Suit.Diamonds;
                return Bid.SuitBid(2, minorSuit);
            }

            return Bid.SuitBid(2, suitToBid);
        }

        if (levelNeeded == 2)
        {
            return null;
        }
        
        return Bid.SuitBid(1, suitToBid);
    }
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        var nextBidLevel = GetNextSuitBidLevel((Suit)bid.Suit!, ctx.AuctionEvaluation.CurrentContract);
        if (bid.Level != nextBidLevel)
            return false;

        if (bid.Type != BidType.Suit)
            return false;

        return true;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();

        if (bid.Level == 2)
            constraints.Add(new HcpConstraint(10, 30));
        
        if (bid.Level == 2 && bid.Suit == Suit.Hearts || bid.Suit == Suit.Spades)
            constraints.Add(new SuitLengthConstraint(bid.Suit, 5, 10));

        if (bid.Level == 1)
        {
            constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 10));
            constraints.Add(new HcpConstraint(6, 30));
        }
        
        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        
    }
}