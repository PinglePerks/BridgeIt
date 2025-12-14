using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Players;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.Gameplay.Table;

public class BiddingTable(
    IAuctionRules rules,
    IBiddingObserver observer
    )
{
    public async Task<AuctionHistory> RunAuction(
        IReadOnlyDictionary<Seat, Hand> hands,
        IReadOnlyDictionary<Seat, IPlayer> players,
        Seat dealer,
        CancellationToken token = default)
    {
        var current = dealer;
        
        var auctionHistory = new AuctionHistory(dealer);
        
        var vulnerability = Vulnerability.None;
        
        while (true)
        {
            if (token.IsCancellationRequested) 
                token.ThrowIfCancellationRequested();
            
            var context = new BiddingContext(hands[current], auctionHistory, current, vulnerability);
            
            var bid = await players[current].GetBidAsync(context);
            
            if (token.IsCancellationRequested) 
                token.ThrowIfCancellationRequested();
            
            auctionHistory.Add(new AuctionBid(current, bid));
            
            observer.OnBid(auctionHistory);
            
            if (rules.ShouldStop(auctionHistory.Bids))
                break;

            current = current.GetNextSeat();
        }

        return auctionHistory;
    }


    

  
}
