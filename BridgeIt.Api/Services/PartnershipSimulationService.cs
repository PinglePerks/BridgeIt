using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Players;

namespace BridgeIt.Api.Services;

public class PartnershipSimulationService
{
    private readonly IEnumerable<IBiddingRule> _rules;
    private readonly IRuleLookupService _ruleLookupService;
    private readonly ILoggerFactory _loggerFactory;

    public PartnershipSimulationService(
        IEnumerable<IBiddingRule> rules,
        IRuleLookupService ruleLookupService,
        ILoggerFactory loggerFactory)
    {
        _rules = rules;
        _ruleLookupService = ruleLookupService;
        _loggerFactory = loggerFactory;
    }

    public async Task<SimulationResult> Simulate(
        Dictionary<Seat, Hand> hands,
        List<string> playedAuction,
        Seat dealer,
        Vulnerability vulnerability,
        (Seat Seat1, Seat Seat2) ourSeats)
    {
        var observer = new CollectingEngineObserver();
        var logger = _loggerFactory.CreateLogger<BiddingEngine>();
        var engine = new BiddingEngine(_rules, logger, observer, new BidValidityChecker());
        var validityChecker = new BidValidityChecker();

        // Parse the played auction into per-seat bid queues for ReplayPlayers
        var opponentBids = BuildOpponentBidQueues(playedAuction, dealer, ourSeats);

        // Wire up players: RobotPlayer for our seats, ReplayPlayer for opponents
        var replayPlayers = new Dictionary<Seat, ReplayPlayer>();
        var players = new Dictionary<Seat, IPlayer>();

        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
        {
            if (seat == ourSeats.Seat1 || seat == ourSeats.Seat2)
            {
                players[seat] = new RobotPlayer(engine, _ruleLookupService);
            }
            else
            {
                var replay = new ReplayPlayer(opponentBids[seat], validityChecker);
                replayPlayers[seat] = replay;
                players[seat] = replay;
            }
        }

        // Run the auction using the standard BiddingTable infrastructure
        var noopObserver = new NoopBiddingObserver();
        var auctionRules = new StandardAuctionRules();
        var table = new BiddingTable(auctionRules, noopObserver);
        var history = await table.RunAuction(hands, players, dealer);

        // Build the bid DTOs with source tagging
        var bids = new List<SimulatedBidDto>();
        var engineLogIndex = 0;

        for (var i = 0; i < history.Bids.Count; i++)
        {
            var auctionBid = history.Bids[i];
            var isOurSeat = auctionBid.Seat == ourSeats.Seat1 || auctionBid.Seat == ourSeats.Seat2;

            string? ruleName = null;
            string? explanation = null;

            if (isOurSeat && engineLogIndex < observer.Logs.Count)
            {
                var log = observer.Logs[engineLogIndex];
                var winningRule = log.EvaluatedRules.FirstOrDefault(r => r.WasSelected);
                if (winningRule != null)
                {
                    ruleName = winningRule.RuleName;
                    explanation = BuildExplanation(winningRule);
                }
                engineLogIndex++;
            }

            bids.Add(new SimulatedBidDto(
                Seat: auctionBid.Seat.ToString(),
                Call: auctionBid.Bid.ToString(),
                Source: isOurSeat ? "engine" : "played",
                RuleName: ruleName,
                Explanation: explanation));
        }

        // Collect conflicts from all ReplayPlayers
        var conflicts = replayPlayers.Values
            .SelectMany(rp => rp.Conflicts)
            .Select(c => new ConflictNoteDto(c.Seat.ToString(), c.RealBid, c.Reason))
            .ToList();

        return new SimulationResult(history, bids, observer.Logs, conflicts);
    }

    /// <summary>
    /// Walks the played auction and extracts the bids belonging to each opponent seat
    /// in the order they were played, skipping our seats' bids.
    /// </summary>
    private static Dictionary<Seat, List<Bid>> BuildOpponentBidQueues(
        List<string> playedAuction, Seat dealer, (Seat Seat1, Seat Seat2) ourSeats)
    {
        var queues = new Dictionary<Seat, List<Bid>>();
        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
        {
            if (seat != ourSeats.Seat1 && seat != ourSeats.Seat2)
                queues[seat] = new List<Bid>();
        }

        var currentSeat = dealer;
        foreach (var bidStr in playedAuction)
        {
            if (currentSeat != ourSeats.Seat1 && currentSeat != ourSeats.Seat2)
            {
                Bid bid;
                try { bid = bidStr.ToBid(); }
                catch { bid = Bid.Pass(); }
                queues[currentSeat].Add(bid);
            }
            currentSeat = currentSeat.GetNextSeat();
        }

        return queues;
    }

    private static string BuildExplanation(RuleEvaluation rule)
    {
        var parts = new List<string> { rule.RuleName };
        if (rule.ForwardConstraints != null)
        {
            foreach (var c in rule.ForwardConstraints)
            {
                if (!string.IsNullOrEmpty(c.Description))
                    parts.Add(c.Description);
            }
        }
        return string.Join(" — ", parts);
    }

    private class NoopBiddingObserver : IBiddingObserver
    {
        public Task OnBid(AuctionHistory auctionHistory) => Task.CompletedTask;
    }

    public record SimulationResult(
        AuctionHistory AuctionHistory,
        List<SimulatedBidDto> Bids,
        List<RuleEvaluationLog> DebugLogs,
        List<ConflictNoteDto> Conflicts);
}
