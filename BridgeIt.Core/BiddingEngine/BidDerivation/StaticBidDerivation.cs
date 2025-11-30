using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class StaticBidDerivation : IBidDerivation
{
    private readonly Bid _bid;
    
    public StaticBidDerivation(Bid bid) => _bid = bid;
    
    public Bid? DeriveBid(BiddingContext ctx) => _bid;
}