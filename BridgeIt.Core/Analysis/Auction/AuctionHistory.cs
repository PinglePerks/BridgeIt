using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Auction;

public class AuctionHistory
{
    private readonly List<AuctionBid> _bids = [];   // internal mutable list

    public IReadOnlyList<AuctionBid> Bids => _bids;  // exposed as read-only
    
    public Seat Dealer { get; }

    public AuctionHistory(Seat dealer)
    {
        Dealer = dealer;
    }

    public void Add(AuctionBid bid)
    {
        _bids.Add(bid);
    }
    
}

public static class AuctionHistoryExtensions
{
    public static List<Bid> GetAllBidsFromSeat(this AuctionHistory auctionHistory, Seat seat) 
        => auctionHistory.Bids.Where(x => x.Seat == seat).Select(x => x.Bid).ToList();
    public static Bid? GetLastBidFromSeat(this AuctionHistory auctionHistory, Seat seat) 
        => auctionHistory.Bids.Where(x => x.Seat == seat).Select(x => x.Bid).LastOrDefault();
}
