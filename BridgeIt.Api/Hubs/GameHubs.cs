using BridgeIt.Api.Models;
using BridgeIt.Api.Services;
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
        _gameService.Players[Context.ConnectionId] = seat;
        await Clients.Caller.SendAsync("PlayerIdentified", seat);
    }

    public async void GenerateScenarioDeal(string scenario)
    {
        if (scenario == "puppetstayman")
        {
            _gameService.GetPuppetStaymanDeal();
            foreach (var connection in _gameService.Players)
            {
                string connectionId = connection.Key;
                Seat seat = connection.Value;

                // This now pulls from the SAME deck
                Hand hand = _gameService.GetHandForPlayer(seat); 

                await Clients.Client(connectionId).SendAsync("ReceiveHand", hand);
            }
        }
        
    }
    
    

    public async Task DealTheCards()
    {
        _gameService.DealNewHand();

        await SendBidHistory();
        // 2. Now distribute the slices of that single truth
        foreach (var connection in _gameService.Players)
        {
            string connectionId = connection.Key;
            Seat seat = connection.Value;

            // This now pulls from the SAME deck
            Hand hand = _gameService.GetHandForPlayer(seat); 

            await Clients.Client(connectionId).SendAsync("ReceiveHand", hand);
        }
        
        await NextPlayer();
    }

    public async Task ConstrainedCardDeal(CustomDealRequest customDealRequest)
    {
        _gameService.DealCustom(customDealRequest);
        
        // 2. Now distribute the slices of that single truth
        foreach (var connection in _gameService.Players)
        {
            string connectionId = connection.Key;
            Seat seat = connection.Value;

            // This now pulls from the SAME deck
            Hand hand = _gameService.GetHandForPlayer(seat); 

            await Clients.Client(connectionId).SendAsync("ReceiveHand", hand);
        }

        await NextPlayer();
    }

    public async Task MakeBid(string bidStr)
    {
        var seat = _gameService.Players[Context.ConnectionId];
    
        // 1. CHECK TURN
        if (!_gameService.IsTurn(seat))
        {
            await Clients.Caller.SendAsync("SystemMessage", "It is not your turn!");
            return;
        }

        var bid = bidStr.ToBid();

        // 2. CHECK VALIDITY
        if (!_gameService.IsValidBid(bid))
        {
            await Clients.Caller.SendAsync("SystemMessage", "Invalid Bid (Insufficient rank or illegal double).");
            return;
        }
        if (_gameService.IsAuctionOver)
        {
            await Clients.All.SendAsync("AuctionEnded", _gameService.HighestBid);
            return;
        }

        // 3. PROCESS
        _gameService.ProcessBid(seat, bid);
    
        // 4. BROADCAST
        await SendBidHistory();
        await NextPlayer();

        // 5. CHECK FOR END OF AUCTION

    }

    public async Task NextPlayer()
    {
        if (_gameService.IsAuctionOver)
        {
            await Clients.All.SendAsync("AuctionEnded", _gameService.HighestBid);
            // Trigger play phase...
        }
        else
        {
            if (_gameService.IsRobotTurn())
            {
                await DoBotBid(_gameService.NextBidder);
            }
            

            await Clients.All.SendAsync("UpdateNextBidder", _gameService.NextBidder);
        }

    }

    public async Task DoBotBid(Seat seat)
    {
        var bid = _gameService.GetBotBid();
        _gameService.ProcessBid(seat, bid);

        await SendBidHistory();
        
        
        
        await NextPlayer();
        
        
    }

    public async Task SendBidHistory()
    {
        await Clients.All.SendAsync("BidHistory",  _gameService.GetBidHistoryDto());
    }
    
    
    
    

}