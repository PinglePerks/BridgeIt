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
    
    
    public BiddingContext(
        Hand hand,
        AuctionHistory auctionHistory,
        Seat seat,
        Vulnerability vulnerability)
    {
        Hand = hand;
        AuctionHistory = auctionHistory;
        Seat = seat;
        Vulnerability = vulnerability;
    }
}

public enum Vulnerability
{
    None,
    EW,
    NS,
    Both
}