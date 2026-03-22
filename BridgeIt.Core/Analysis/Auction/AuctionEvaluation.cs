using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;


namespace BridgeIt.Core.Analysis.Auction;

public class AuctionEvaluation
{
    public SeatRoleType SeatRoleType { get; init; }
    public Bid? CurrentContract { get; init; }
    public Bid? OpeningBid { get; init; }
    public Bid? PartnerLastBid { get; init; }

    /// <summary>Last non-pass bid made by partner (skips passes)</summary>
    public Bid? PartnerLastNonPassBid { get; init; }

    /// <summary>Last non-pass bid made by the current seat</summary>
    public Bid? MyLastNonPassBid { get; init; }

    public Seat? OpeningSeat {get; init;}
    public Seat? NextSeatToBid { get; init; }
    public AuctionPhase AuctionPhase { get; init; }
    public int BiddingRound { get; init; }
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
            PartnerLastBid = GetPartnerLastBid(auctionHistory),
            PartnerLastNonPassBid = GetPartnerLastNonPassBid(auctionHistory),
            MyLastNonPassBid = GetMyLastNonPassBid(auctionHistory),
            OpeningSeat = GetOpeningSeat(auctionHistory),
            AuctionPhase = GetAuctionPhase(auctionHistory),
            BiddingRound = GetBiddingRound(auctionHistory),
        };
    }
    
    private static int GetBiddingRound(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
    
        var openingBidIndex = history.Bids
            .Select((b, i) => (b, i))
            .FirstOrDefault(x => x.b.Bid.Type != BidType.Pass).i;
    
        // PreOpening — no one has opened yet
        if (history.Bids.All(b => b.Bid.Type == BidType.Pass)) return 0;

        // Count every turn (pass or bid) the current seat has had since the opening bid
        var turnsSinceOpening = history.Bids
            .Skip(openingBidIndex)
            .Count(b => b.Seat == currentSeat);

        return turnsSinceOpening + 1;
    }

    
    private static Bid? GetPartnerLastBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        var partnerSeat = currentSeat.GetPartner();
        return history.Bids.LastOrDefault(b => b.Seat == partnerSeat)?.Bid;
    }

    private static Bid? GetPartnerLastNonPassBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        var partnerSeat = currentSeat.GetPartner();
        return history.Bids
            .LastOrDefault(b => b.Seat == partnerSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }

    private static Bid? GetMyLastNonPassBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        return history.Bids
            .LastOrDefault(b => b.Seat == currentSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }
    
    private static AuctionPhase GetAuctionPhase(AuctionHistory history)
    {
        var nonPassBids = history.Bids.Where(b => b.Bid.Type != BidType.Pass).ToList();
        if (!nonPassBids.Any()) return AuctionPhase.PreOpening;

        var openingSeat = nonPassBids.First().Seat;
        var oppositionHasBid = nonPassBids.Any(b => b.Seat != openingSeat && b.Seat != openingSeat.GetPartner());
    
        return oppositionHasBid ? AuctionPhase.Contested : AuctionPhase.Uncontested;
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

public enum AuctionPhase
{
    PreOpening,    // No non-pass bids have been made
    Uncontested,   // One side has opened; opponents have only passed
    Contested,     // Both sides have made non-pass bids
    //Terminated    // Auction closed (3+ consecutive passes after a contract exists)
}