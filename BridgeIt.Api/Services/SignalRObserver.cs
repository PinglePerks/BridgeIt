using BridgeIt.Api.Hubs;
using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Output;
using Microsoft.AspNetCore.SignalR;

namespace BridgeIt.Api.Services;

public class SignalRBiddingObserver : IBiddingObserver
{
    private readonly IHubContext<GameHub> _hubContext;

    public SignalRBiddingObserver(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void OnBid(AuctionHistory auctionHistory)
    {
        // Broadcast to all clients immediately
        _hubContext.Clients.All.SendAsync("BidHistory",  auctionHistory.Bids.Select(b => new BidDto((int)b.Seat, b.Bid.ToString())).ToList());
    }
    
}