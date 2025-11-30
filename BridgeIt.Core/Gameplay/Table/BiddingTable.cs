using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Services;

namespace BridgeIt.Core.Gameplay.Table;

public sealed class BiddingTable(
    BiddingEngine.Core.BiddingEngine engine,
    IAuctionRules rules,
    ISeatRotationService rotation,
    IBiddingObserver observer
    )
{
    
    public IReadOnlyList<BiddingDecision> RunAuction(
        IReadOnlyDictionary<Seat, Hand> hands,
        Seat dealer)
    {
        var auctionHistory = new AuctionHistory(new List<BiddingDecision>(), dealer);
        var current = dealer;

        while (true)
        {
            var ctx = new BiddingContext(
                hand: hands[current],
                auctionHistory: auctionHistory,
                seat: current,
                vulnerability: Vulnerability.None,
                handEvaluation: HandEvaluator.Evaluate(hands[current]),
                partnershipKnowledge: AuctionEvaluator.AnalyzeKnowledge(auctionHistory, current, hands[current]),
                auctionEvaluation: AuctionEvaluator.Evaluate(auctionHistory, current)
                );

            var decision = engine.ChooseBid(ctx);
            Console.WriteLine(decision.ChosenBid);

            auctionHistory.Add(decision);

            //observer.OnBid(current, bid);

            if (rules.ShouldStop(auctionHistory.Bids))
                break;

            current = rotation.Next(current);
        }

        return auctionHistory.Bids;
    }
}
