using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;

namespace BridgeIt.Core.BiddingEngine.Core;

public class DecisionContext
{
    public BiddingContext Data { get; init; }
    public HandEvaluation HandEvaluation { get; init; }
    public PartnershipKnowledge PartnershipKnowledge { get; init; }
    public AuctionEvaluation AuctionEvaluation { get; init; }

    public DecisionContext(BiddingContext data, HandEvaluation handEvaluation, AuctionEvaluation auctionEvaluation, PartnershipKnowledge partnershipKnowledge)
    {
        Data = data;
        HandEvaluation = handEvaluation;
        AuctionEvaluation = auctionEvaluation;
        PartnershipKnowledge = partnershipKnowledge;
    }

    public DecisionContext(BiddingContext data)
    {
        Data = data;
        HandEvaluation = HandEvaluator.Evaluate(data.Hand);
        AuctionEvaluation = AuctionEvaluator.Evaluate(data.AuctionHistory);
        PartnershipKnowledge = new PartnershipKnowledge();
    }
}