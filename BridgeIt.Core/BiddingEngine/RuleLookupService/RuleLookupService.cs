using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.RuleLookupService;

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
            var currentBidder = bid.Seat;

            // 3. Build TableKnowledge from the perspective of the current bidder,
            //    then apply cross-table inferences so each player's constraints
            //    are tightened by what we know about all other players.
            var tableKnowledge = new TableKnowledge(currentBidder);
            foreach (var (seat, bidInfos) in derivedConstraints)
            {
                if (seat != currentBidder)
                {
                    tableKnowledge.Players[seat] = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);
                }
            }

            // Cross-table HCP inference: we don't know the bidder's actual hand here,
            // so we use their current inferred HcpMin as a conservative lower bound.
            var bidderKnowledge = PlayerKnowledgeEvaluator.AnalyzeKnowledge(derivedConstraints[currentBidder]);
            tableKnowledge.ApplyCrossTableInferences(bidderKnowledge.HcpMin);

            // Extract partnership bidding state from partner's last bid info
            var partnerBids = derivedConstraints[currentBidder.GetPartner()];
            var partnershipState = partnerBids.LastOrDefault()?.PartnershipBiddingState
                                   ?? PartnershipBiddingState.Unknown;

            // 4. Construct Context
            // Note: We use an empty hand/evaluation because we are analyzing history,
            // not making a decision for the current user's hand.
            var bidContext = new BiddingContext(null!, replayHistory, currentBidder, ctx.Vulnerability);
            var decisionContext = new DecisionContext(bidContext, new HandEvaluation(),
                AuctionEvaluator.Evaluate(replayHistory), tableKnowledge, partnershipState);

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
