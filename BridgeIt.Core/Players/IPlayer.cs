using System.Net.Http.Headers;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Players;


public interface IPlayer
{
    Task<Bid> GetBidAsync(BiddingContext context);
}

public class HumanPlayer : IPlayer
{
    private TaskCompletionSource<Bid> _bidTcs;

    // This is called by the BiddingTable loop
    public Task<Bid> GetBidAsync(BiddingContext context)
    {
        // Reset the TCS for a new turn
        _bidTcs = new TaskCompletionSource<Bid>();
        
        // You might want to trigger an event here to notify the UI 
        // via SignalR that it is this player's turn.
        // OnTurnNeeded?.Invoke(this, context.Seat);

        return _bidTcs.Task;
    }

    // This is called by the GameHub when a bid is received from the client
    public void SetBid(Bid bid)
    {
        if (_bidTcs != null && !_bidTcs.Task.IsCompleted)
        {
            _bidTcs.SetResult(bid);
        }
    }
}

public class RobotPlayer(BiddingEngine.Core.BiddingEngine engine,
    IRuleLookupService ruleLookupService) : IPlayer
{
    public async Task<Bid> GetBidAsync(BiddingContext context)
    {
        var handEval = HandEvaluator.Evaluate(context.Hand);
        
        var auctionEval = AuctionEvaluator.Evaluate(context.AuctionHistory);
        
        var constraints = ruleLookupService.GetConstraintsFromBids(context, engine);
        
        var partnerShipKnowledge = PartnershipEvaluator.AnalyzeKnowledge(constraints[context.Seat.GetPartner()]);
        
        var decisionContext = new DecisionContext(context, handEval, auctionEval, partnerShipKnowledge);
        
        return engine.ChooseBid(decisionContext);
    }
}

