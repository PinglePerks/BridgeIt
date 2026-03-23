using BridgeIt.Api.Models;
using BridgeIt.Api.Services;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }

    // ─── Standard game flow ───────────────────────────────────────────────────

    public async Task IdentifyPlayer(Seat seat)
    {
        _gameService.AddHumanPlayer(Context.ConnectionId, seat);
        await Clients.Caller.SendAsync("PlayerIdentified", seat);
    }

    public async Task DealCards()
    {
        _gameService.DealNewHand();
        foreach (var connection in _gameService.ConnectionMap)
        {
            var hand = _gameService.GetHandForPlayer(connection.Value);
            await Clients.Client(connection.Key).SendAsync("ReceiveHand", hand);
        }
    }

    public async Task StartGame()
    {
        await _gameService.StartGame();
    }

    public async Task MakeBid(string bidStr)
    {
        if (_gameService.ReceiveHumanBid(Context.ConnectionId, bidStr.ToBid()))
            await Clients.Caller.SendAsync("BidMadeSuccessfully", bidStr);
        else
            await Clients.Caller.SendAsync("UnsuccessfulBid", bidStr);
    }

    public async Task TestHandString()
    {
        var hands = _gameService.GetAllHands();
        var fullString = string.Join('\n', hands.Select(h => $"{h.Key}:{h.Value}"));
        await Clients.Caller.SendAsync("HandsString", fullString);
    }

    // ─── Test / debug mode ────────────────────────────────────────────────────

    /// <summary>Auto-seats the caller as North, deals randomly, starts auction.</summary>
    public async Task StartTestGame()
    {
        _gameService.AddHumanPlayer(Context.ConnectionId, Seat.North);
        await Clients.Caller.SendAsync("PlayerIdentified", Seat.North);
        await _gameService.StartGame();
        // Always broadcast all hands so TestGameTable can show the full deal
        await Clients.Caller.SendAsync("GetAllHands", _gameService.GetAllHands());
    }

    /// <summary>Starts a fully-robotic game. Caller observes without a seat.</summary>
    public async Task StartObserverGame()
    {
        await _gameService.StartObserverGame(Context.ConnectionId);
        await Clients.Caller.SendAsync("GetAllHands", _gameService.GetAllHands());
    }

    /// <summary>Deals a named scenario from ScenarioRegistry.</summary>
    public async Task DealScenario(string scenarioKey)
    {
        if (!ScenarioRegistry.All.TryGetValue(scenarioKey, out var scenario))
        {
            await Clients.Caller.SendAsync("SystemMessage", $"Unknown scenario: {scenarioKey}");
            return;
        }
        await _gameService.DealScenario(scenario);
        await Clients.Caller.SendAsync("GetAllHands", _gameService.GetAllHands());
    }

    /// <summary>Deals a hand meeting bespoke HCP/shape constraints for North.</summary>
    public async Task DealBespoke(BespokeConstraintDto constraints)
    {
        try
        {
            await _gameService.DealBespoke(constraints);
            await Clients.Caller.SendAsync("GetAllHands", _gameService.GetAllHands());
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SystemMessage", $"Could not generate hand: {ex.Message}");
        }
    }

    /// <summary>Returns the list of available scenarios to the caller.</summary>
    public async Task GetScenarios()
    {
        var scenarios = ScenarioRegistry.All
            .Select(kv => new { key = kv.Key, name = kv.Value.DisplayName, category = kv.Value.Category })
            .GroupBy(s => s.category)
            .Select(g => new { category = g.Key, items = g.ToList() })
            .ToList();
        await Clients.Caller.SendAsync("ReceiveScenarios", scenarios);
    }

    public async Task TestAllHands()
    {
        await Clients.Caller.SendAsync("GetAllHands", _gameService.GetAllHands());
    }
}
