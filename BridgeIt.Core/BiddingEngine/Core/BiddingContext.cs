using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Core;

public class BiddingContext
{
    //immutable / raw data
    public Hand Hand { get; }
    public AuctionHistory AuctionHistory { get; }
    public Seat Seat { get; }
    public Vulnerability Vulnerability { get; }
    
    //derived data
    public AuctionEvaluation AuctionEvaluation { get; }
    public HandEvaluation HandEvaluation { get; }
    public PartnershipKnowledge PartnershipKnowledge { get; } = new();
    
    public BiddingContext(
        Hand hand,
        AuctionHistory auctionHistory,
        Seat seat,
        Vulnerability vulnerability,
        HandEvaluation handEvaluation,
        PartnershipKnowledge partnershipKnowledge,
        AuctionEvaluation auctionEvaluation)
    {
        Hand = hand;
        AuctionHistory = auctionHistory;
        Seat = seat;
        HandEvaluation = handEvaluation;
        AuctionEvaluation = auctionEvaluation;
        Vulnerability = vulnerability;
        PartnershipKnowledge = partnershipKnowledge;
    }
}

public enum Vulnerability
{
    None,
    EW,
    NS,
    Both
}