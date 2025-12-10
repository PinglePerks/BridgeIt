using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;


namespace BridgeIt.Core.Analysis.Auction;

public class AuctionEvaluation
{
    public SeatRoleType SeatRoleType { get; init; }
    public Bid? CurrentContract { get; init; }
    public Bid? OpeningBid { get; init; }
    public Bid? PartnerLastBid { get; init; }
    public Seat? OpeningSeat {get; init;}
    public Seat? NextSeatToBid { get; init; }
}

public static class AuctionEvaluator
{
    public static AuctionEvaluation Evaluate(AuctionHistory auctionHistory)
    {

        return new AuctionEvaluation()
        {
            NextSeatToBid = GetNextSeatToBid(auctionHistory),
            CurrentContract = GetCurrentContract(auctionHistory),
            SeatRoleType = GetSeatRole(auctionHistory),
            OpeningBid = GetOpeningBid(auctionHistory),
            PartnerLastBid = PartnerLastBid(auctionHistory),
            OpeningSeat = GetOpeningSeat(auctionHistory)
        };
    }
    
    private static Bid? PartnerLastBid(AuctionHistory history)
    {
        if (history.Bids.Count < 2) return null;
        return history.Bids[^2].Bid;
    }
    
    private static Seat GetNextSeatToBid(AuctionHistory auctionHistory)
    {
        var auctionBid = auctionHistory.Bids.LastOrDefault();
        
        return auctionBid == null ? auctionHistory.Dealer : auctionBid.Seat.GetNextSeat();
    }
    
    
    private static Bid? GetCurrentContract(AuctionHistory auctionHistory)
        => auctionHistory.Bids
            .LastOrDefault(x => x.Bid.Type == BidType.NoTrumps || x.Bid.Type == BidType.Suit)?
            .Bid;
    
    private static Seat? GetOpeningSeat(AuctionHistory auctionHistory) 
        => auctionHistory.Bids.FirstOrDefault(x => x.Bid.Type != BidType.Pass)?.Seat;
    
    private static Bid? GetOpeningBid(AuctionHistory auctionHistory) 
        => auctionHistory.Bids.FirstOrDefault(x => x.Bid.Type != BidType.Pass)?.Bid;


    public static SeatRoleType GetSeatRole(AuctionHistory auctionHistory)
    {
        var currentSeat = GetNextSeatToBid(auctionHistory);
        
        var openingSeat = GetOpeningSeat(auctionHistory);
        if (openingSeat == null) return SeatRoleType.NoBids;
        
        if (openingSeat == currentSeat) return SeatRoleType.Opener;
        
        var difference = ((int)currentSeat - (int)openingSeat + 4) % 4;

        return difference switch
        {
            1 => SeatRoleType.Overcaller,
            2 => SeatRoleType.Responder,
            3 => SeatRoleType.Overcaller,
            _ => throw new ArgumentOutOfRangeException()
        };
        
    }
}