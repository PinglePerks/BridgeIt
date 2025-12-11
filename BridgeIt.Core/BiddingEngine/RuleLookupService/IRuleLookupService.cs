using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.RuleLookupService;

public interface IRuleLookupService
{
    public Dictionary<Seat, List<BidInformation>> GetConstraintsFromBids(BiddingContext ctx, Core.BiddingEngine engine);

}

public class RuleLookupService : IRuleLookupService
{
    public Dictionary<Seat, List<BidInformation>> GetConstraintsFromBids(BiddingContext ctx, Core.BiddingEngine engine)
    {
        var dictConstraints = new Dictionary<Seat, List<BidInformation>>
        {
            { Seat.North, new List<BidInformation>() },
            { Seat.East, new List<BidInformation>() },
            { Seat.South, new List<BidInformation>() },
            { Seat.West, new List<BidInformation>() },
        };
        var auctionHistory = new AuctionHistory(ctx.AuctionHistory.Dealer);
        
        foreach (var bid in ctx.AuctionHistory.Bids)
        {
            var biddingContext = new BiddingContext(ctx.Hand, auctionHistory, bid.Seat, ctx.Vulnerability);

            var handEval = new HandEvaluation();
            var auctionEval = AuctionEvaluator.Evaluate(auctionHistory);
            var constraints = GetConstraintsFromBids(biddingContext, engine);
            var partnerConstraints = constraints[bid.Seat.GetPartner()];
            var partnershipKnowledge = PartnershipEvaluator.AnalyzeKnowledge(partnerConstraints);
            
            
            var knownCtx = new DecisionContext(biddingContext, handEval, auctionEval, partnershipKnowledge);
            
            var bidInformation = engine.GetConstraintsFromBid(knownCtx, bid.Bid);

            if (bidInformation != null)
            {
                dictConstraints[bid.Seat].Add(bidInformation);
            }
              
            auctionHistory.Add(bid);
        }
        return dictConstraints;

    }
}