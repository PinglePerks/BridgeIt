using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.RuleLookupService;

public interface IRuleLookupService
{
    public Dictionary<Seat, List<(IBidConstraint, string)>> GetConstraintsFromBids(IReadOnlyList<AuctionBid> bidHistory, Vulnerability vulnerability, Seat dealer, Core.BiddingEngine engine);

}

public class RuleLookupService : IRuleLookupService
{
    public Dictionary<Seat, List<(IBidConstraint, string)>> GetConstraintsFromBids(IReadOnlyList<AuctionBid> bidHistory, Vulnerability vulnerability, Seat dealer, Core.BiddingEngine engine)
    {
        var bids = new List<AuctionBid>();
        
        var dictConstraints = new Dictionary<Seat, List<(IBidConstraint, string)>>
        {
            { Seat.North, new List<(IBidConstraint, string)>() },
            { Seat.East, new List<(IBidConstraint, string)>() },
            { Seat.South, new List<(IBidConstraint, string)>() },
            { Seat.West, new List<(IBidConstraint, string)>() },
        };

        var nextState = "";
        
        foreach (var bid in bidHistory)
        {
            var auctionHistory = new AuctionHistory(bids, dealer);
            
            var knownCtx = new BiddingContext(
                new Hand(new List<Card>()),
                auctionHistory,
                bid.Seat,
                vulnerability,
                HandEvaluator.Evaluate(new Hand(new List<Card>())),
                new PartnershipKnowledge(),
                AuctionEvaluator.Evaluate(auctionHistory, bid.Seat, nextState));
            
            var constraint = engine.GetConstraintsFromBid(knownCtx, bid.Decision.ChosenBid);

            if (constraint != (null, null))
            {
                dictConstraints[bid.Seat].Add(constraint);
                nextState = constraint.Item2.ToString();
            }
              
            bids.Add(bid);
        }
        return dictConstraints;

    }
}