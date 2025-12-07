using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Services;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.Gameplay.Table;

public class BiddingTable(
    BiddingEngine.Core.BiddingEngine engine,
    IAuctionRules rules,
    ISeatRotationService rotation,
    IBiddingObserver observer,
    ILogger<BiddingTable> logger,
    IRuleLookupService ruleLookupService
    )
{
    public IReadOnlyList<AuctionBid> RunAuction(
        IReadOnlyDictionary<Seat, Hand> hands,
        Seat dealer)
    {
        var auctionHistory = new AuctionHistory(new List<AuctionBid>(), dealer);
        var current = dealer;

        while (true)
        {
            var ctx = engine.CreateBiddingContext(current, hands[current], auctionHistory, dealer, rotation, ruleLookupService);
            
            logger.LogDebug($"Evaluating {current} hand");
            
            var decision = engine.ChooseBid(ctx);
            
            var auctionBid = new AuctionBid(current, decision );
            
            auctionHistory.Add(auctionBid);

            observer.OnBid(current, decision);

            if (rules.ShouldStop(auctionHistory.Bids))
                break;

            current = rotation.Next(current);
        }

        return auctionHistory.Bids;
    }


    

  
}
