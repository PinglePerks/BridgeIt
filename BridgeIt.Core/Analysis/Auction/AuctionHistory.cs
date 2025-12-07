using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Auction;

public class AuctionHistory
{
    private readonly List<AuctionBid> _bids;   // internal mutable list

    public IReadOnlyList<AuctionBid> Bids => _bids;  // exposed as read-only
    
    public Seat Dealer { get; }

    public Seat? OpeningBidder()
    {
        for (var i =0; i < _bids.Count; i++)
        {
            if (_bids[i].Decision.ChosenBid.Type != BidType.Pass) return SeatAtIndex(i);
        }

        return null;
    }

    public AuctionHistory(
        List<AuctionBid> bids,
        Seat dealer)
    {
        _bids = bids;
        Dealer = dealer;
    }

    public void Add(AuctionBid bid)
    {
        _bids.Add(bid);
    }

    public int Count => _bids.Count;
    
    public Seat SeatAtIndex(int index) =>
        (Seat)(((int)Dealer + index) % 4);
    
    public List<BiddingDecision> GetAllSeatBids(Seat seat)
    {
        return _bids.Where(b => b.Seat == seat).Select(b => b.Decision).ToList();
    }
    
    

    public List<BiddingDecision> GetAllPartnerBids(Seat currentSeat)
    {
        // Partner is always 2 seats away in a 4-player game
        int partnerSeatIndex = ((int)currentSeat + 2) % 4;
        
        return _bids.Where(b => b.Seat == (Seat)partnerSeatIndex).Select(b => b.Decision).ToList();
    }

    public Seat LastSeat =>
        Count == 0 ? Dealer : SeatAtIndex(Count - 1);
}
