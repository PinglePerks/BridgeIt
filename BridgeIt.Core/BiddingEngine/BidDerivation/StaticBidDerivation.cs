using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.BidDerivation;

public class StaticBidDerivation(Bid bid) : IBidDerivation
{
    public Bid? DeriveBid(DecisionContext ctx) => bid;
    public bool CanProduceBid(Bid bid1, DecisionContext ctx)
        => bid1 == bid;
}