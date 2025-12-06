using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Auction;

public class AuctionHistory
{
    private readonly List<BiddingDecision> _bids;   // internal mutable list

    public IReadOnlyList<BiddingDecision> Bids => _bids;  // exposed as read-only
    
    public Seat Dealer { get; }

    public Seat? OpeningBidder()
    {
        for (var i =0; i < _bids.Count; i++)
        {
            if (_bids[i].ChosenBid.Type != BidType.Pass) return SeatAtIndex(i);
        }

        return null;
    }

    public AuctionHistory(
        List<BiddingDecision> bids,
        Seat dealer)
    {
        _bids = bids;
        Dealer = dealer;
    }

    public void Add(BiddingDecision bid)
    {
        _bids.Add(bid);
    }

    public int Count => _bids.Count;
    
    public Seat SeatAtIndex(int index) =>
        (Seat)(((int)Dealer + index) % 4);
    
    public List<BiddingDecision> GetAllSeatBids(Seat seat)
    {
        var seatBids = new List<BiddingDecision>();
        
        // Partner is always 2 seats away in a 4-player game
        int seatIndex = ((int)seat) % 4;

        for (int i = 0; i < _bids.Count; i++)
        {
            // Check if the seat at this index matches the partner's seat index
            if ((int)SeatAtIndex(i) == seatIndex)
            {
                seatBids.Add(_bids[i]);
            }
        }

        return seatBids;
    }
    
    

    public List<BiddingDecision> GetAllPartnerBids(Seat currentSeat)
    {
        var partnerBids = new List<BiddingDecision>();
        
        // Partner is always 2 seats away in a 4-player game
        int partnerSeatIndex = ((int)currentSeat + 2) % 4;

        for (int i = 0; i < _bids.Count; i++)
        {
            // Check if the seat at this index matches the partner's seat index
            if ((int)SeatAtIndex(i) == partnerSeatIndex)
            {
                partnerBids.Add(_bids[i]);
            }
        }
        
        return partnerBids;
    }

    public Seat LastSeat =>
        Count == 0 ? Dealer : SeatAtIndex(Count - 1);
}
