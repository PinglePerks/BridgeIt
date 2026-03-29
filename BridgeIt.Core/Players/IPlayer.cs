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
    Task<BidResult> GetBidAsync(BiddingContext context);

    event EventHandler<Seat> OnTurn;
}

public class HumanPlayer : IPlayer
{
    public AuctionHistory? CurrentHistory { get; private set; }
    private TaskCompletionSource<BidResult> _bidTcs;

    // This is called by the BiddingTable loop
    public Task<BidResult> GetBidAsync(BiddingContext context)
    {
        CurrentHistory = context.AuctionHistory;
        // Reset the TCS for a new turn
        _bidTcs = new TaskCompletionSource<BidResult>();

        OnTurn?.Invoke(this, context.Seat);

        return _bidTcs.Task;
    }

    public event EventHandler<Seat>? OnTurn;

    // This is called by the GameHub when a bid is received from the client
    public void SetBid(Bid bid)
    {
        if (_bidTcs != null && !_bidTcs.Task.IsCompleted)
        {
            _bidTcs.SetResult(new BidResult(bid));
        }
    }
}

/// <summary>
/// Replays a pre-recorded sequence of bids. Used in partnership simulation
/// to inject opponents' real bids into a mixed engine/replay auction.
/// Reports conflicts when a replayed bid is invalid at that auction state.
/// </summary>
public class ReplayPlayer : IPlayer
{
    private readonly Queue<Bid> _bids;
    private readonly List<ConflictRecord> _conflicts = new();
    private readonly Domain.IBidValidityChecker.IBidValidityChecker _validityChecker;

    public IReadOnlyList<ConflictRecord> Conflicts => _conflicts;

    public ReplayPlayer(IEnumerable<Bid> bids, Domain.IBidValidityChecker.IBidValidityChecker? validityChecker = null)
    {
        _bids = new Queue<Bid>(bids);
        _validityChecker = validityChecker ?? new Domain.IBidValidityChecker.BidValidityChecker();
    }

    public Task<BidResult> GetBidAsync(BiddingContext context)
    {
        if (_bids.Count == 0)
            return Task.FromResult(new BidResult(Bid.Pass()));

        var bid = _bids.Dequeue();

        var auctionBid = new AuctionBid(context.Seat, bid);
        if (!_validityChecker.IsValid(auctionBid, context.AuctionHistory))
        {
            _conflicts.Add(new ConflictRecord(
                context.Seat, bid.ToString(),
                $"Bid '{bid}' is not valid at this point in the auction"));
        }

        // Inject anyway — phase 1 doesn't resolve conflicts
        return Task.FromResult(new BidResult(bid));
    }

    public event EventHandler<Seat>? OnTurn;

    public record ConflictRecord(Seat Seat, string RealBid, string Reason);
}

public class RobotPlayer(BiddingEngine.Core.BiddingEngine engine,
    IRuleLookupService ruleLookupService) : IPlayer
{
    public async Task<BidResult> GetBidAsync(BiddingContext context)
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

