using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public interface IBidDerivation
{
    Bid DeriveBid(BiddingContext ctx);
}

public class LengthBidDerivation : IBidDerivation
{
    public Bid DeriveBid(BiddingContext ctx)
    {
        var suit 
            = ctx.HandEvaluation.Shape.OrderByDescending(s => s.Value).First().Key;
        
        var length = ctx.HandEvaluation.Shape[suit];

        var bidLevel = length - 4;

        return Bid.SuitBid(bidLevel, suit);
    }
}

public class SimpleRaise : IBidDerivation
{
    private int _level { get; init; }
    public SimpleRaise(int level)
    {
        _level = level;
    }
    public Bid DeriveBid(BiddingContext ctx)
    {
        var suit  = ctx.PartnershipKnowledge.FitInSuit;
        //if(suit == null) return Bid.NoTrumpsBid(5);

        return Bid.SuitBid(_level, suit!.Value);
    }
}