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
        var lastNonPassBid = auctionHistory.Bids.LastOrDefault(x => x.Bid.Type != BidType.Pass);
        var lastContractBid = auctionHistory.Bids.LastOrDefault(x => x.Bid.Type != BidType.Pass && x.Bid.Type != BidType.Redouble && x.Bid.Type != BidType.Double);
        var newBid = bid.Bid;
        
        if (newBid.Type == BidType.Pass) return true;
        
        var lastBid = lastNonPassBid?.Bid;
        var lastBidder = lastNonPassBid?.Seat;

        if (newBid.Type == BidType.Double)
        {
            if(lastBidder == null) return false;
            if(lastBidder.Value.GetPartner() == bid.Seat) return false;
            return true;
        }
        
        if (newBid.Type == BidType.Redouble)
        {
            var lastAuctionBid = lastNonPassBid?.Bid;
            if(lastAuctionBid == null) return false;
            if (lastBidder!.Value.GetPartner() == bid.Seat ||
                lastBid!.Type != BidType.Double) return false;
            return true;
        }
;
        if(lastBid == null) return true;
        
        return NewBidHigherThanLastBid(lastContractBid!.Bid, newBid);
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