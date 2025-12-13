using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class OneLevelResponderBidDerivation() : BidDerivationBase
{

    public override Bid DeriveBid(DecisionContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        var currentBid = ctx.AuctionEvaluation.CurrentContract;
        
        //find suit to bid
        var numHearts = ctx.HandEvaluation.Shape[Suit.Hearts];
        var numSpades = ctx.HandEvaluation.Shape[Suit.Spades];
        if (numHearts >= numSpades && Math.Max(numHearts, numSpades) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Hearts, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Hearts);
        }

        if (Math.Max(numSpades, numHearts) >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Spades, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Spades);
        }

        if (ctx.HandEvaluation.Shape[Suit.Diamonds] >= 4)
        {
            var nxt = GetNextSuitBidLevel(Suit.Diamonds, currentBid);
            if (nxt == 1) return Bid.SuitBid(nxt, Suit.Diamonds);
        }

        return Bid.NoTrumpsBid(1);
    }
}