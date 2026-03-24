using System.Collections.Concurrent;
using BridgeIt.Api.Hubs;
using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Players;
using BridgeIt.Dealer.HandSpecifications;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Services;

public class GameService
{
    // Maps ConnectionId -> Seat (for UI identification)
    public ConcurrentDictionary<string, Seat> ConnectionMap = new();

    // Maps Seat -> Actual Player Logic (Human or Robot)
    private readonly Dictionary<Seat, IPlayer> _players = new();

    // The current deal
    private Dictionary<Seat, Hand> _currentDeal = new();

    public List<BidDto> BidHistoryDto = new();

    private readonly BiddingEngine _biddingEngine;
    private readonly IBidValidityChecker _bidValidityChecker;
    private readonly IRuleLookupService _lookup;
    private readonly BiddingTable _table;
    private readonly IHubContext<GameHub> _hubContext;

    private CancellationTokenSource? _gameCts;

    public GameService(
        BiddingEngine biddingEngine,
        IBidValidityChecker bidValidityChecker,
        BiddingTable table,
        IRuleLookupService ruleLookupService,
        IHubContext<GameHub> hubContext)
    {
        _biddingEngine = biddingEngine;
        _bidValidityChecker = bidValidityChecker;
        _table = table;
        _lookup = ruleLookupService;
        _hubContext = hubContext;

        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
            _players[seat] = new RobotPlayer(_biddingEngine, _lookup);
    }

    // ─── Player registration ──────────────────────────────────────────────────

    public void AddHumanPlayer(string connectionId, Seat seat)
    {
        ConnectionMap[connectionId] = seat;
        var human = new HumanPlayer();
        human.OnTurn += async (_, _) =>
            await _hubContext.Clients.Client(connectionId).SendAsync("UpdateNextBidder", seat);
        _players[seat] = human;
    }

    public void RemoveHumanPlayer(string connectionId)
    {
        if (ConnectionMap.TryRemove(connectionId, out var seat))
            _players[seat] = new RobotPlayer(_biddingEngine, _lookup);
    }

    public bool ReceiveHumanBid(string connectionId, Bid bid)
    {
        if (!ConnectionMap.TryGetValue(connectionId, out var seat)) return false;
        if (_players[seat] is not HumanPlayer human) return false;

        var auctionBid = new AuctionBid(seat, bid);
        if (!_bidValidityChecker.IsValid(auctionBid, human.CurrentHistory)) return false;

        human.SetBid(bid);
        return true;
    }

    // ─── Game start variants ──────────────────────────────────────────────────

    /// <summary>Random deal with current player registrations.</summary>
    public async Task StartGame()
    {
        await CancelExistingGame();
        DealNewHand();
        await NotifyPlayersOfNewDeal();
        LaunchAuction();
    }

    /// <summary>
    /// All-robot game. Removes the connection from the seat map so it observes only.
    /// </summary>
    public async Task StartObserverGame(string connectionId)
    {
        RemoveHumanPlayer(connectionId);
        await StartGame();
    }

    /// <summary>Deal using a named scenario from ScenarioRegistry.</summary>
    public async Task DealScenario(ScenarioDeal scenario)
    {
        await CancelExistingGame();
        var dealer = new BridgeIt.Dealer.Deal.Dealer();
        _currentDeal = dealer.GenerateConstrainedDeal(
            scenario.North,
            scenario.East,
            scenario.South,
            scenario.West);
        BidHistoryDto.Clear();
        await NotifyPlayersOfNewDeal();
        LaunchAuction();
    }

    /// <summary>Deal using bespoke HCP/shape constraints for North. Others pass.</summary>
    public async Task DealBespoke(BespokeConstraintDto dto)
    {
        await CancelExistingGame();

        var northConstraint = HandSpecification.Generator(
            dto.MinHcp, dto.MaxHcp,
            minShape: new Dictionary<Suit, int>
            {
                [Suit.Spades] = dto.MinSpades,
                [Suit.Hearts] = dto.MinHearts,
                [Suit.Diamonds] = dto.MinDiamonds,
                [Suit.Clubs] = dto.MinClubs,
            },
            maxShape: new Dictionary<Suit, int>
            {
                [Suit.Spades] = dto.MaxSpades,
                [Suit.Hearts] = dto.MaxHearts,
                [Suit.Diamonds] = dto.MaxDiamonds,
                [Suit.Clubs] = dto.MaxClubs,
            },
            longestAndStronger: false); // shape only, no longest-suit requirement

        var dealer = new BridgeIt.Dealer.Deal.Dealer();
        _currentDeal = dealer.GenerateConstrainedDeal(
            northConstraint,
            HandSpecification.PassingOpponent,
            HandSpecification.PassingOpponent,
            HandSpecification.PassingOpponent);
        BidHistoryDto.Clear();
        await NotifyPlayersOfNewDeal();
        LaunchAuction();
    }

    /// <summary>Parse a multi-line hand string and deal those exact cards.</summary>
    /// <remarks>
    /// Expected format (one line per seat, any order):
    ///   North: Q43/A982/AKQ75/Q
    ///   East: AJT/KQ73/T93/T42
    ///   South: K7652//862/KJ865
    ///   West: 98/JT654/J4/A973
    /// Each hand is Spades/Hearts/Diamonds/Clubs with ranks like A K Q J T 9 8 7 6 5 4 3 2.
    /// </remarks>
    public async Task DealExactHands(string handText)
    {
        await CancelExistingGame();

        var deal = ParseHandText(handText);
        _currentDeal = deal;
        BidHistoryDto.Clear();
        await NotifyPlayersOfNewDeal();
        LaunchAuction();
    }

    /// <summary>Restart the auction with the current deal (same cards, fresh auction).</summary>
    public async Task RestartAuction()
    {
        if (_currentDeal.Count == 0)
            throw new InvalidOperationException("No deal to restart.");

        await CancelExistingGame();
        BidHistoryDto.Clear();
        await NotifyPlayersOfNewDeal();
        LaunchAuction();
    }

    private static Dictionary<Seat, Hand> ParseHandText(string handText)
    {
        var result = new Dictionary<Seat, Hand>();
        var lines = handText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;

            var seatStr = line[..colonIdx].Trim();
            var handStr = line[(colonIdx + 1)..].Trim();

            if (!Enum.TryParse<Seat>(seatStr, ignoreCase: true, out var seat))
                throw new ArgumentException($"Unknown seat: '{seatStr}'");

            var suits = handStr.Split('/');
            if (suits.Length != 4)
                throw new ArgumentException($"Hand for {seat} must have exactly 4 suit groups separated by '/' (Spades/Hearts/Diamonds/Clubs), got {suits.Length}");

            var cards = new List<Card>();
            var suitOrder = new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
            for (int i = 0; i < 4; i++)
            {
                foreach (var ch in suits[i])
                {
                    if (char.IsWhiteSpace(ch)) continue;
                    cards.Add(new Card(suitOrder[i], ParseRank(ch)));
                }
            }

            result[seat] = new Hand(cards);
        }

        if (result.Count != 4)
            throw new ArgumentException($"Expected hands for all 4 seats, got {result.Count}: {string.Join(", ", result.Keys)}");

        // Validate total card count
        var totalCards = result.Values.Sum(h => h.Cards.Count);
        if (totalCards != 52)
            throw new ArgumentException($"Expected 52 cards total, got {totalCards}");

        return result;
    }

    private static Rank ParseRank(char ch) => char.ToUpper(ch) switch
    {
        'A' => Rank.Ace,
        'K' => Rank.King,
        'Q' => Rank.Queen,
        'J' => Rank.Jack,
        'T' => Rank.Ten,
        '9' => Rank.Nine,
        '8' => Rank.Eight,
        '7' => Rank.Seven,
        '6' => Rank.Six,
        '5' => Rank.Five,
        '4' => Rank.Four,
        '3' => Rank.Three,
        '2' => Rank.Two,
        _ => throw new ArgumentException($"Unknown rank character: '{ch}'")
    };

    // ─── Internals ────────────────────────────────────────────────────────────

    private async Task CancelExistingGame()
    {
        if (_gameCts == null) return;
        await _gameCts.CancelAsync();
        _gameCts.Dispose();
        _gameCts = null;
    }

    private void LaunchAuction()
    {
        _gameCts = new CancellationTokenSource();
        var token = _gameCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await _table.RunAuction(_currentDeal, _players, Seat.North, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Auction error: {ex.Message}");
            }
        }, token);
    }

    private async Task NotifyPlayersOfNewDeal()
    {
        await _hubContext.Clients.All.SendAsync("ResetTable");
        foreach (var connection in ConnectionMap)
        {
            var seat = connection.Value;
            if (_currentDeal.TryGetValue(seat, out var hand))
                await _hubContext.Clients.Client(connection.Key).SendAsync("ReceiveHand", hand);
        }
    }

    // ─── Queries ──────────────────────────────────────────────────────────────

    public Hand GetHandForPlayer(Seat seat) => _currentDeal[seat];
    public Dictionary<Seat, Hand> GetAllHands() => _currentDeal;
    public List<BidDto> GetBidHistoryDto() => BidHistoryDto;

    public void DealNewHand()
    {
        _currentDeal = new BridgeIt.Dealer.Deal.Dealer().GenerateRandomDeal();
        BidHistoryDto.Clear();
    }
}
