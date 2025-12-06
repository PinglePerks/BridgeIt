using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class TransferBidDerivation : BidDerivationBase
{
    public override Bid DeriveBid(BiddingContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        var level = partnerBid!.Level!;
        var suit = partnerBid.Suit!;

        var transfer = (int)suit + 1;
        
        if (transfer == 4)
        {
            transfer = 0 ;
            level++;
        }
        
        return Bid.SuitBid(level, (Suit)transfer);
    }
}