using System.Collections.Concurrent;
using BridgeIt.Api.Hubs;
using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Table;
using BridgeIt.Core.Players;
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
    
    // Auction History (for UI display)
    // Ideally, this should come from BiddingTable result, but we can mirror it here 
    // or expose it via an Observer.
    public List<BidDto> BidHistoryDto = new(); 

    private readonly BiddingEngine _biddingEngine;
    private readonly IBidValidityChecker _bidValidityChecker;
    private readonly IRuleLookupService _lookup;
    private readonly BiddingTable _table;
    private readonly IHubContext<GameHub> _hubContext;
    
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

        // Initialize all seats as Robots by default
        foreach (Seat seat in Enum.GetValues(typeof(Seat)))
        {
            _players[seat] = new RobotPlayer(_biddingEngine, _lookup);
        }
    }

    public void AddHumanPlayer(string connectionId, Seat seat)
    {
        ConnectionMap[connectionId] = seat;
        // Replace the robot with a HumanPlayer for this seat

        var human = new HumanPlayer();
        
        human.OnTurn += async (sender, s) => 
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("UpdateNextBidder", seat);
        };
        
        _players[seat] = human;
    }

    public void ReceiveHumanBid(string connectionId, Bid bid)
    {
        if (ConnectionMap.TryGetValue(connectionId, out var seat))
        {
            if (_players[seat] is HumanPlayer human)
            {
                // Completes the Task awaiting in BiddingTable
                human.SetBid(bid);
            }
        }
    }

    public async Task StartGame()
    {
        // 1. Deal

        // 2. Run the Table Loop
        // This is a long-running async method that waits for humans
        var history = await _table.RunAuction(_currentDeal, _players, Seat.North);
        
        // 3. Update History for UI (post-game, or rely on Observer for live updates)
        UpdateBidHistory(history);
    }

    public Hand GetHandForPlayer(Seat seat) => _currentDeal[seat];

    public void DealNewHand()
    {
        // ... (Your dealing logic) ...
        var dealer = new BridgeIt.Dealer.Deal.Dealer();
        _currentDeal = dealer.GenerateRandomDeal();
        BidHistoryDto.Clear();
    }

    // Helper to get DTOs for the UI
    public List<BidDto> GetBidHistoryDto() => BidHistoryDto;

    private void UpdateBidHistory(AuctionHistory history)
    {
        BidHistoryDto = history.Bids.Select(b => new BidDto((int)b.Seat, b.Bid.ToString())).ToList();
    }
}