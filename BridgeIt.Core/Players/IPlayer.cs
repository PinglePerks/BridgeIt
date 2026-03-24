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
    
    event EventHandler<Seat> OnTurn;
}

public class HumanPlayer : IPlayer
{
    public AuctionHistory? CurrentHistory { get; private set; }
    private TaskCompletionSource<Bid> _bidTcs;

    // This is called by the BiddingTable loop
    public Task<Bid> GetBidAsync(BiddingContext context)
    {
        CurrentHistory = context.AuctionHistory;
        // Reset the TCS for a new turn
        _bidTcs = new TaskCompletionSource<Bid>();
        
        OnTurn?.Invoke(this, context.Seat);

        return _bidTcs.Task;
    }

    public event EventHandler<Seat>? OnTurn;

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

        // Build TableKnowledge from all seats' inferred constraints.
        // Me is populated too — it represents "what I've shown through my bids"
        // so rules can compare Me vs HandEvaluation to find hidden strength/length.
        var tableKnowledge = new TableKnowledge(context.Seat);
        foreach (var (seat, bidInfos) in constraints)
        {
            tableKnowledge.Players[seat] = PlayerKnowledgeEvaluator.AnalyzeKnowledge(bidInfos);
        }
        tableKnowledge.ApplyCrossTableInferences(handEval.Hcp);
        tableKnowledge.ApplyCrossTableSuitInferences(handEval.Shape);

        // Extract partnership bidding state from partner's last bid info
        var partnerBids = constraints[context.Seat.GetPartner()];
        var partnershipState = partnerBids.LastOrDefault()?.PartnershipBiddingState
                               ?? PartnershipBiddingState.Unknown;

        var decisionContext = new DecisionContext(context, handEval, auctionEval, tableKnowledge, partnershipState);

        return engine.ChooseBid(decisionContext);
    }
    
    public event EventHandler<Seat>? OnTurn;
}

