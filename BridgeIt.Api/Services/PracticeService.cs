using System.Collections.Concurrent;
using BridgeIt.Api.Hubs;
using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Players;
using BridgeIt.Dds;
using BridgeIt.Dealer.HandSpecifications;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Services;

public class PracticeSession
{
    public string SessionId { get; init; } = "";
    public PracticeConfigDto Config { get; init; } = null!;
    public Seat HostSeat { get; init; }
    public Seat GuestSeat { get; init; }

    public string? HostConnectionId { get; set; }
    public string? GuestConnectionId { get; set; }

    public Dictionary<Seat, IPlayer> Players { get; } = new();
    public Dictionary<Seat, Hand> CurrentDeal { get; set; } = new();
    public int HandNumber { get; set; }
    public List<PracticeHandSummary> HandHistory { get; } = new();
    public CancellationTokenSource? AuctionCts { get; set; }
    public AuctionHistory? LastAuctionHistory { get; set; }
    public bool AuctionComplete { get; set; }
}

public class PracticeService
{
    private readonly ConcurrentDictionary<string, PracticeSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _connectionToSession = new();
    private volatile string? _activeSessionId;

    private readonly BiddingEngine _biddingEngine;
    private readonly IBidValidityChecker _bidValidityChecker;
    private readonly IRuleLookupService _lookup;
    private readonly IAuctionRules _auctionRules;
    private readonly IHubContext<PracticeHub> _hubContext;
    private readonly IDdsService _ddsService;

    public PracticeService(
        BiddingEngine biddingEngine,
        IBidValidityChecker bidValidityChecker,
        IRuleLookupService ruleLookupService,
        IAuctionRules auctionRules,
        IHubContext<PracticeHub> hubContext,
        IDdsService ddsService)
    {
        _biddingEngine = biddingEngine;
        _bidValidityChecker = bidValidityChecker;
        _lookup = ruleLookupService;
        _auctionRules = auctionRules;
        _hubContext = hubContext;
        _ddsService = ddsService;
    }

    public PracticeSession CreateSession(string connectionId, PracticeConfigDto config)
    {
        // Remove any stale sessions so GetActiveSession() always returns the newest one
        if (_activeSessionId != null)
        {
            if (_sessions.TryRemove(_activeSessionId, out var old))
                old.AuctionCts?.Cancel();
            _activeSessionId = null;
        }

        var hostSeat = (Seat)config.HostSeat;
        var guestSeat = hostSeat.GetPartner();
        var sessionId = Guid.NewGuid().ToString("N")[..8];

        var session = new PracticeSession
        {
            SessionId = sessionId,
            Config = config,
            HostSeat = hostSeat,
            GuestSeat = guestSeat,
            HostConnectionId = connectionId,
        };

        // Set up robots for non-human seats
        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
            session.Players[seat] = new RobotPlayer(_biddingEngine, _lookup);

        // Register host as human
        var hostHuman = new HumanPlayer();
        hostHuman.OnTurn += async (_, _) =>
        {
            if (session.HostConnectionId != null)
                await _hubContext.Clients.Client(session.HostConnectionId).SendAsync("UpdateNextBidder", hostSeat);
        };
        session.Players[hostSeat] = hostHuman;

        _sessions[sessionId] = session;
        _connectionToSession[connectionId] = sessionId;
        _activeSessionId = sessionId;

        return session;
    }

    public PracticeSession? GetSessionForConnection(string connectionId)
    {
        if (_connectionToSession.TryGetValue(connectionId, out var sessionId))
            return _sessions.GetValueOrDefault(sessionId);
        return null;
    }

    public PracticeSession? GetActiveSession()
    {
        if (_activeSessionId != null && _sessions.TryGetValue(_activeSessionId, out var session))
            return session;
        return _sessions.Values.FirstOrDefault();
    }

    public PracticeSession? JoinSession(string connectionId)
    {
        var session = GetActiveSession();
        if (session == null) return null;

        session.GuestConnectionId = connectionId;
        _connectionToSession[connectionId] = session.SessionId;

        // Register guest as human
        var guestHuman = new HumanPlayer();
        guestHuman.OnTurn += async (_, _) =>
        {
            if (session.GuestConnectionId != null)
                await _hubContext.Clients.Client(session.GuestConnectionId).SendAsync("UpdateNextBidder", session.GuestSeat);
        };
        session.Players[session.GuestSeat] = guestHuman;

        return session;
    }

    public async Task DealNextHand(string connectionId)
    {
        var session = GetSessionForConnection(connectionId);
        if (session == null) return;

        await CancelExistingAuction(session);

        session.HandNumber++;
        session.AuctionComplete = false;
        session.LastAuctionHistory = null;

        // Deal based on situation config
        var dealer = new BridgeIt.Dealer.Deal.Dealer();
        session.CurrentDeal = DealForSituation(dealer, session.Config.Situation, session.HostSeat);

        // Broadcast reset and hands
        var connectionIds = GetSessionConnectionIds(session);
        foreach (var connId in connectionIds)
            await _hubContext.Clients.Client(connId).SendAsync("ResetTable");

        // Send each human their hand
        if (session.HostConnectionId != null && session.CurrentDeal.TryGetValue(session.HostSeat, out var hostHand))
            await _hubContext.Clients.Client(session.HostConnectionId).SendAsync("ReceiveHand", hostHand);
        if (session.GuestConnectionId != null && session.CurrentDeal.TryGetValue(session.GuestSeat, out var guestHand))
            await _hubContext.Clients.Client(session.GuestConnectionId).SendAsync("ReceiveHand", guestHand);

        // Send session state
        foreach (var connId in connectionIds)
            await _hubContext.Clients.Client(connId).SendAsync("HandStarted", new { session.HandNumber, Dealer = "North" });

        LaunchAuction(session);
    }

    public async Task RestartAuction(string connectionId)
    {
        var session = GetSessionForConnection(connectionId);
        if (session == null || session.CurrentDeal.Count == 0) return;

        await CancelExistingAuction(session);
        session.AuctionComplete = false;
        session.LastAuctionHistory = null;

        // Fresh HumanPlayer instances so bid history is clean
        ResetHumanPlayers(session);

        var connectionIds = GetSessionConnectionIds(session);
        foreach (var connId in connectionIds)
            await _hubContext.Clients.Client(connId).SendAsync("ResetTable");

        if (session.HostConnectionId != null && session.CurrentDeal.TryGetValue(session.HostSeat, out var hostHand))
            await _hubContext.Clients.Client(session.HostConnectionId).SendAsync("ReceiveHand", hostHand);
        if (session.GuestConnectionId != null && session.CurrentDeal.TryGetValue(session.GuestSeat, out var guestHand))
            await _hubContext.Clients.Client(session.GuestConnectionId).SendAsync("ReceiveHand", guestHand);

        foreach (var connId in connectionIds)
            await _hubContext.Clients.Client(connId).SendAsync("HandStarted", new { session.HandNumber, Dealer = "North" });

        LaunchAuction(session);
    }

    private void ResetHumanPlayers(PracticeSession session)
    {
        var hostHuman = new HumanPlayer();
        hostHuman.OnTurn += async (_, _) =>
        {
            if (session.HostConnectionId != null)
                await _hubContext.Clients.Client(session.HostConnectionId).SendAsync("UpdateNextBidder", session.HostSeat);
        };
        session.Players[session.HostSeat] = hostHuman;

        if (session.GuestConnectionId != null)
        {
            var guestHuman = new HumanPlayer();
            guestHuman.OnTurn += async (_, _) =>
            {
                if (session.GuestConnectionId != null)
                    await _hubContext.Clients.Client(session.GuestConnectionId).SendAsync("UpdateNextBidder", session.GuestSeat);
            };
            session.Players[session.GuestSeat] = guestHuman;
        }
    }

    public bool ReceiveBid(string connectionId, Bid bid)
    {
        var session = GetSessionForConnection(connectionId);
        if (session == null) return false;

        Seat seat;
        HumanPlayer? human;

        if (connectionId == session.HostConnectionId)
        {
            seat = session.HostSeat;
            human = session.Players[seat] as HumanPlayer;
        }
        else if (connectionId == session.GuestConnectionId)
        {
            seat = session.GuestSeat;
            human = session.Players[seat] as HumanPlayer;
        }
        else return false;

        if (human == null) return false;

        var auctionBid = new AuctionBid(seat, bid);
        if (!_bidValidityChecker.IsValid(auctionBid, human.CurrentHistory)) return false;

        human.SetBid(bid);
        return true;
    }

    public void RemoveConnection(string connectionId)
    {
        _connectionToSession.TryRemove(connectionId, out _);
    }

    public PracticeSessionInfo ToSessionInfo(PracticeSession session)
    {
        return new PracticeSessionInfo(
            session.SessionId,
            (int)session.HostSeat,
            (int)session.GuestSeat,
            session.Config.Situation,
            session.Config.Conventions,
            session.Config.HandLimit,
            session.GuestConnectionId != null
        );
    }

    private static Dictionary<Seat, Hand> DealForSituation(BridgeIt.Dealer.Deal.Dealer dealer, string situation, Seat hostSeat)
    {
        // null constraint = any hand (no filter)
        return situation switch
        {
            "nt-auction" => dealer.GenerateConstrainedDeal(
                HandSpecification.Open1NT, null, null, null),
            "major-fit" => dealer.GenerateConstrainedDeal(
                HandSpecification.AcolMajor1LevelOpening(Suit.Spades),
                null,
                // Partner has 4+ card support
                h => ShapeEvaluator.GetShape(h).GetValueOrDefault(Suit.Spades, 0) >= 4 &&
                     HighCardPoints.Count(h) >= 6,
                null),
            "slam-potential" => dealer.GenerateConstrainedDeal(
                h => HighCardPoints.Count(h) >= 16 && HighCardPoints.Count(h) <= 22,
                null,
                h => HighCardPoints.Count(h) >= 14 && HighCardPoints.Count(h) <= 22,
                null),
            "competitive" => dealer.GenerateConstrainedDeal(
                h => HighCardPoints.Count(h) >= 12 && HighCardPoints.Count(h) <= 19,
                h => HighCardPoints.Count(h) >= 10 && HighCardPoints.Count(h) <= 16,
                null, null),
            "major-opening" => dealer.GenerateConstrainedDeal(
                HandSpecification.OneLevelUnbalancedOpening,
                null, null, null),
            _ => dealer.GenerateRandomDeal()
        };
    }

    private void LaunchAuction(PracticeSession session)
    {
        session.AuctionCts = new CancellationTokenSource();
        var token = session.AuctionCts.Token;

        // Create a practice-specific bidding observer that broadcasts to session only
        var observer = new PracticeBiddingObserver(_hubContext, session);

        var table = new BiddingTable(_auctionRules, observer);

        _ = Task.Run(async () =>
        {
            try
            {
                var auctionHistory = await table.RunAuction(session.CurrentDeal, session.Players, Seat.North, token);
                session.LastAuctionHistory = auctionHistory;
                session.AuctionComplete = true;

                // Reveal all hands
                var allHands = session.CurrentDeal.ToDictionary(
                    kv => kv.Key.ToString()!,
                    kv => new { cards = kv.Value.Cards });
                var connectionIds = GetSessionConnectionIds(session);
                foreach (var connId in connectionIds)
                    await _hubContext.Clients.Client(connId).SendAsync("AllHandsRevealed", allHands);

                // Compute result
                var result = ComputeHandResult(session);
                if (result != null)
                {
                    session.HandHistory.Add(new PracticeHandSummary(
                        session.HandNumber, result.YourContract, result.OptimumContract, result.Delta));

                    foreach (var connId in connectionIds)
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("HandComplete", result);
                        await _hubContext.Clients.Client(connId).SendAsync("HandHistory", session.HandHistory);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Practice auction error: {ex.Message}");
            }
        }, token);
    }

    private HandResultDto? ComputeHandResult(PracticeSession session)
    {
        if (session.LastAuctionHistory == null) return null;

        var bids = session.LastAuctionHistory.Bids;
        var lastContractBid = bids.LastOrDefault(b => b.Bid.Type == BidType.Suit || b.Bid.Type == BidType.NoTrumps);

        string yourContract;
        int yourScore;

        if (lastContractBid == null)
        {
            yourContract = "Passed out";
            yourScore = 0;
        }
        else
        {
            var declarer = FindDeclarer(bids, lastContractBid);
            yourContract = $"{lastContractBid.Bid} {declarer}";

            // Use DDS to find tricks declarer can make
            var ddsAnalysis = GetDdsAnalysis(session);
            var tricks = GetTricksForContract(ddsAnalysis, lastContractBid, declarer);
            var contractStr = lastContractBid.Bid.ToString();
            yourScore = BridgeScorer.ComputeScore(contractStr, tricks, false) ?? 0;
        }

        // Get par/optimum from DDS
        string optimumContract = "Pass";
        int optimumScore = 0;
        try
        {
            var ddsAnalysis = _ddsService.Analyse(session.CurrentDeal, Seat.North);
            var par = ddsAnalysis.Par.GetValueOrDefault("none");
            if (par != null)
            {
                optimumContract = $"{par.Contract} {par.Declarer}";
                optimumScore = par.Score;
                // Adjust score to be from NS perspective if needed
                if (par.ScoringSide == "EW")
                    optimumScore = -optimumScore;
            }
        }
        catch { /* DDS is non-critical */ }

        var delta = Math.Abs(optimumScore - yourScore);
        var verdict = delta switch
        {
            0 => "optimal",
            <= 50 => "close",
            <= 200 => "inaccurate",
            _ => "poor"
        };

        return new HandResultDto(yourContract, yourScore, optimumContract, optimumScore, delta, verdict);
    }

    private Dds.Models.DdsAnalysis? GetDdsAnalysis(PracticeSession session)
    {
        try { return _ddsService.Analyse(session.CurrentDeal, Seat.North); }
        catch { return null; }
    }

    private static int GetTricksForContract(Dds.Models.DdsAnalysis? dds, AuctionBid contractBid, Seat declarer)
    {
        if (dds == null) return contractBid.Bid.Level + 6; // assume made if no DDS

        var seatKey = declarer switch
        {
            Seat.North => "N", Seat.East => "E", Seat.South => "S", Seat.West => "W",
            _ => "N"
        };

        var strainKey = contractBid.Bid.Type == BidType.NoTrumps ? "notrump" :
            contractBid.Bid.Suit switch
            {
                Suit.Clubs => "clubs",
                Suit.Diamonds => "diamonds",
                Suit.Hearts => "hearts",
                Suit.Spades => "spades",
                _ => "notrump"
            };

        if (dds.TrickTable.Tricks.TryGetValue(seatKey, out var seatTricks) &&
            seatTricks.TryGetValue(strainKey, out var tricks))
            return tricks;

        return contractBid.Bid.Level + 6;
    }

    private static Seat FindDeclarer(IReadOnlyList<AuctionBid> bids, AuctionBid contractBid)
    {
        // Declarer is the first player of the declaring side to bid the contract's strain
        var declaringSide = (int)contractBid.Seat % 2;
        foreach (var bid in bids)
        {
            if ((int)bid.Seat % 2 != declaringSide) continue;
            if (bid.Bid.Type == contractBid.Bid.Type &&
                (bid.Bid.Type == BidType.NoTrumps || bid.Bid.Suit == contractBid.Bid.Suit))
                return bid.Seat;
        }
        return contractBid.Seat;
    }

    private async Task CancelExistingAuction(PracticeSession session)
    {
        if (session.AuctionCts != null)
        {
            await session.AuctionCts.CancelAsync();
            session.AuctionCts.Dispose();
            session.AuctionCts = null;
        }
    }

    private static List<string> GetSessionConnectionIds(PracticeSession session)
    {
        var ids = new List<string>();
        if (session.HostConnectionId != null) ids.Add(session.HostConnectionId);
        if (session.GuestConnectionId != null) ids.Add(session.GuestConnectionId);
        return ids;
    }
}

/// <summary>
/// Broadcasts bid history only to practice session participants.
/// </summary>
public class PracticeBiddingObserver : IBiddingObserver
{
    private readonly IHubContext<PracticeHub> _hubContext;
    private readonly PracticeSession _session;

    public PracticeBiddingObserver(IHubContext<PracticeHub> hubContext, PracticeSession session)
    {
        _hubContext = hubContext;
        _session = session;
    }

    public async Task OnBid(AuctionHistory auctionHistory)
    {
        var bids = auctionHistory.Bids
            .Select(b => new BidDto((int)b.Seat, b.Bid.ToString(), b.IsAlerted))
            .ToList();

        var connectionIds = new List<string>();
        if (_session.HostConnectionId != null) connectionIds.Add(_session.HostConnectionId);
        if (_session.GuestConnectionId != null) connectionIds.Add(_session.GuestConnectionId);

        foreach (var connId in connectionIds)
            await _hubContext.Clients.Client(connId).SendAsync("BidHistory", bids);
    }
}
