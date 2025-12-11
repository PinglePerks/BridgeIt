using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Domain.IBidValidityChecker;

public interface IBidValidityChecker
{
    bool IsValid(AuctionBid bid, AuctionHistory auctionHistory);
}

public class BidValidityChecker : IBidValidityChecker
{
    public bool IsValid(AuctionBid bid, AuctionHistory auctionHistory)
    {
        var newBid = bid.Bid;
        
        if (newBid.Type == BidType.Pass) return true;

        if (newBid.Type == BidType.Double)
        {
            var lastBidder = auctionHistory.Bids
                .LastOrDefault(x => x.Bid.Type != BidType.Pass)?
                .Seat;
            if(lastBidder == null) return false;
            if(lastBidder.Value.GetPartner() == bid.Seat) return false;
            return true;
        }
        
        if (newBid.Type == BidType.Redouble)
        {
            var lastAuctionBid = auctionHistory.Bids
                .LastOrDefault(x => x.Bid.Type != BidType.Pass);
            if(lastAuctionBid == null) return false;
            if (lastAuctionBid.Seat.GetPartner() == bid.Seat ||
                lastAuctionBid.Bid.Type != BidType.Double) return false;
            return true;
        }
        var currentContract = auctionHistory.Bids.LastOrDefault(x => x.Bid.Type != BidType.Pass)?.Bid;
        if(currentContract == null) return true;
        
        return NewBidHigherThanLastBid(currentContract, newBid);
        
    }
    
    private bool NewBidHigherThanLastBid(Bid highestBid, Bid newBid)
    {
        if (newBid.Level > highestBid.Level) return true;
        if (newBid.Level == highestBid.Level)
        {
            if (newBid.Type == BidType.NoTrumps && highestBid.Type == BidType.Suit) return true;
            if (newBid.Suit > highestBid.Suit) return true;
        }
        return false;
    }
}