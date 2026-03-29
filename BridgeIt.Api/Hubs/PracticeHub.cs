using BridgeIt.Api.Models;
using BridgeIt.Api.Services;
using BridgeIt.Core.Domain.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Hubs;

public class PracticeHub : Hub
{
    private readonly PracticeService _practiceService;

    public PracticeHub(PracticeService practiceService)
    {
        _practiceService = practiceService;
    }

    public async Task StartPracticeSession(PracticeConfigDto config)
    {
        var session = _practiceService.CreateSession(Context.ConnectionId, config);
        var info = _practiceService.ToSessionInfo(session);
        await Clients.Caller.SendAsync("SessionCreated", info);
    }

    public async Task JoinPracticeSession()
    {
        var session = _practiceService.JoinSession(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("NoSessionAvailable");
            return;
        }

        var info = _practiceService.ToSessionInfo(session);
        await Clients.Caller.SendAsync("SessionJoined", info);

        // Notify host that guest has joined
        if (session.HostConnectionId != null)
            await Clients.Client(session.HostConnectionId).SendAsync("SessionReady", info);
        await Clients.Caller.SendAsync("SessionReady", info);

        // If a hand is already in progress, catch the guest up
        if (session.HandNumber > 0 && session.CurrentDeal.TryGetValue(session.GuestSeat, out var guestHand))
        {
            await Clients.Caller.SendAsync("ResetTable");
            await Clients.Caller.SendAsync("ReceiveHand", guestHand);
            await Clients.Caller.SendAsync("HandStarted", new { session.HandNumber, Dealer = "North" });

            if (session.AuctionComplete && session.LastAuctionHistory != null)
            {
                // Auction is done — reveal all hands and send result
                var allHands = session.CurrentDeal.ToDictionary(
                    kv => kv.Key.ToString()!, kv => new { cards = kv.Value.Cards });
                await Clients.Caller.SendAsync("AllHandsRevealed", allHands);
            }
        }
    }

    public async Task GetSessionInfo()
    {
        var session = _practiceService.GetActiveSession();
        if (session == null)
        {
            await Clients.Caller.SendAsync("NoSessionAvailable");
            return;
        }
        await Clients.Caller.SendAsync("SessionInfo", _practiceService.ToSessionInfo(session));
    }

    public async Task DealNextHand()
    {
        await _practiceService.DealNextHand(Context.ConnectionId);
    }

    public async Task RestartAuction()
    {
        await _practiceService.RestartAuction(Context.ConnectionId);
    }

    public async Task MakeBid(string bidStr)
    {
        if (_practiceService.ReceiveBid(Context.ConnectionId, bidStr.ToBid()))
            await Clients.Caller.SendAsync("BidMadeSuccessfully", bidStr);
        else
            await Clients.Caller.SendAsync("UnsuccessfulBid", bidStr);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _practiceService.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
