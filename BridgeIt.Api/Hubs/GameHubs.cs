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

    // STEP 1: Client says "I am Player 1"
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
            string connectionId = connection.Key;
            Seat seat = connection.Value;

            Hand hand = _gameService.GetHandForPlayer(seat);

            await Clients.Client(connectionId).SendAsync("ReceiveHand", hand);
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

        var fullString = "";
        foreach (var hand in hands)
        {
            fullString += hand.Key.ToString()+':'+ hand.Value.ToString() + '\n';
        }

        await Clients.Caller.SendAsync("HandsString", fullString);
    }
    
    //******************TEST MODE**********************
    public async Task TestAllHands()
    {
        var hands = _gameService.GetAllHands();
        
        await Clients.Caller.SendAsync("GetAllHands", hands);
    }



    
    
    
    
    

}