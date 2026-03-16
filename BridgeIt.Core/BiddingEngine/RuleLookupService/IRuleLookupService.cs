using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
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
        // 1. Initialize empty knowledge buckets for all seats
        var derivedConstraints = new Dictionary<Seat, List<BidInformation>>
        {
            { Seat.North, new List<BidInformation>() },
            { Seat.East, new List<BidInformation>() },
            { Seat.South, new List<BidInformation>() },
            { Seat.West, new List<BidInformation>() },
        };

        // 2. Replay the auction linearly
        var replayHistory = new AuctionHistory(ctx.AuctionHistory.Dealer);
        
        foreach (var bid in ctx.AuctionHistory.Bids)
        {
            // Identify who is bidding and who is their partner
            var currentBidder = bid.Seat;
            var partner = currentBidder.GetPartner();

            // 3. Build knowledge for the context of THIS specific bid
            
            // A. What does the Bidder know about their Partner?
            // derived from the Partner's previous bids
            var knowledgeOfPartner = PartnershipEvaluator.AnalyzeKnowledge(derivedConstraints[partner]);

            // B. (Recursive Step) What does the Bidder know that Partner knows about the Bidder?
            // derived from the Bidder's OWN previous bids
            var partnerKnowledgeOfMe = PartnershipEvaluator.AnalyzeKnowledge(derivedConstraints[currentBidder]);
            
            // C. Link them
            knowledgeOfPartner.PartnerKnowledgeOfMe = partnerKnowledgeOfMe;

            // 4. Construct Context
            // Note: We use an empty hand/evaluation because we are analyzing history, 
            // not making a decision for the current user's hand.
            var bidContext = new BiddingContext(null!, replayHistory, currentBidder, ctx.Vulnerability);
            var decisionContext = new DecisionContext(bidContext, new HandEvaluation(), AuctionEvaluator.Evaluate(replayHistory), knowledgeOfPartner);

            // 5. Ask Engine: "Given this context, what does this Bid mean?"
            var bidInfo = engine.GetConstraintsFromBid(decisionContext, bid.Bid);

            // 6. Store the result
            if (bidInfo != null)
            {
                derivedConstraints[currentBidder].Add(bidInfo);
            }

            // 7. Advance the history for the next loop iteration
            replayHistory.Add(bid);
        }

        return derivedConstraints;
    }
}