using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.RuleLookupService;

public interface IRuleLookupService
{
    public Dictionary<Seat, List<IBidConstraint>> GetConstraintsFromBids(IReadOnlyList<AuctionBid> bidHistory, Vulnerability vulnerability, Seat dealer, Core.BiddingEngine engine);

}

public class RuleLookupService : IRuleLookupService
{
    public Dictionary<Seat, List<IBidConstraint>> GetConstraintsFromBids(IReadOnlyList<AuctionBid> bidHistory, Vulnerability vulnerability, Seat dealer, Core.BiddingEngine engine)
    {
        var bids = new List<AuctionBid>();
        
        var dictConstraints = new Dictionary<Seat, List<IBidConstraint>>
        {
            { Seat.North, new List<IBidConstraint>() },
            { Seat.East,  new List<IBidConstraint>() },
            { Seat.South, new List<IBidConstraint>() },
            { Seat.West,  new List<IBidConstraint>() }
        };
        
        
        foreach (var bid in bidHistory)
        {
            var auctionHistory = new AuctionHistory(bids, dealer);
            
            var knownCtx = new BiddingContext(
                new Hand(new List<Card>()),
                auctionHistory,
                bid.Seat,
                vulnerability,
                new HandEvaluation(),
                new PartnershipKnowledge(),
                AuctionEvaluator.Evaluate(auctionHistory, bid.Seat));
            
            var constraint = engine.GetConstraintsFromBid(knownCtx, bid.Decision.ChosenBid);
            
            if (constraint != null)
                dictConstraints[bid.Seat].Add(constraint);
            bids.Add(bid);
        }
        return dictConstraints;

    }
}