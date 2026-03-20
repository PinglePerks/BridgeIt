using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.IBidValidityChecker;

namespace BridgeIt.Core.BiddingEngine.Core;

public class DecisionContext
{
    public BiddingContext Data { get; init; }
    public HandEvaluation HandEvaluation { get; init; }
    public PartnershipKnowledge PartnershipKnowledge { get; init; }
    public AuctionEvaluation AuctionEvaluation { get; init; }
    
    public IBidValidityChecker ValidityChecker { get; }

    public DecisionContext(BiddingContext data, HandEvaluation handEvaluation, AuctionEvaluation auctionEvaluation, PartnershipKnowledge partnershipKnowledge)
    {
        Data = data;
        HandEvaluation = handEvaluation;
        AuctionEvaluation = auctionEvaluation;
        PartnershipKnowledge = partnershipKnowledge;
        ValidityChecker = new BidValidityChecker();
    }
}