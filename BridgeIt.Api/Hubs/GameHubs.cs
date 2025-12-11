using BridgeIt.Api.Models;
using BridgeIt.Api.Services;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;
    
    private Dictionary<Seat, Hand> _currentDeal;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }

    // STEP 1: Client says "I am Player 1"
    public async void IdentifyPlayer(Seat seat)
    {
        _gameService.AddHumanPlayer(Context.ConnectionId, seat);
        await Clients.Caller.SendAsync("PlayerIdentified", seat);
    }


    public async Task DealCards()
    {
        _gameService.DealNewHand();
        foreach (var connection in _gameService.ConnectionMap)
        {
            string connectionId = connection.Key;
            Seat seat = connection.Value;

            // This now pulls from the SAME deck
            Hand hand = _gameService.GetHandForPlayer(seat); 

            await Clients.Client(connectionId).SendAsync("ReceiveHand", hand);
        }
        
    }

    public async Task StartGame()
    {
        _gameService.StartGame();
    }

    public async Task MakeBid(string bidStr)
    {
        _gameService.ReceiveHumanBid(Context.ConnectionId, bidStr.ToBid());
    }



    
    
    
    
    

}