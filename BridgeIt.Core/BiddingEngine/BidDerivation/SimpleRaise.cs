using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class SimpleRaise : BidDerivationBase
{
    private int _level { get; init; }
    public SimpleRaise(int level)
    {
        _level = level;
    }
    public override Bid DeriveBid(BiddingContext ctx)
    {
        var suit  = ctx.PartnershipKnowledge.FitInSuit;
        //if(suit == null) return Bid.NoTrumpsBid(5);

        return Bid.SuitBid(_level, suit!.Value);
    }
}