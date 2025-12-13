using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class LengthBidDerivation(string type, string? forbidden) : BidDerivationBase
{
    public override Bid? DeriveBid(DecisionContext ctx)
    {
        if (forbidden == null) return CalculateBid(ctx);
        var forbiddenBid = forbidden.ToBid();
        var bid = CalculateBid(ctx);
        if (bid.Level != forbiddenBid.Level && bid.Suit != forbiddenBid.Suit) return bid;
        return null;
    }

    protected internal virtual Bid CalculateBid(DecisionContext ctx)
    {
        var suit 
            = ctx.HandEvaluation.Shape.OrderByDescending(s => s.Value).First().Key;
        
        var length = ctx.HandEvaluation.Shape[suit];
        if (ctx.HandEvaluation.Hcp > 9 && type == "overcall")
            return Bid.SuitBid(GetNextSuitBidLevel(suit, ctx.AuctionEvaluation.CurrentContract), suit);

        var bidLevel = length - 4;

        return Bid.SuitBid(bidLevel, suit);
    }
}