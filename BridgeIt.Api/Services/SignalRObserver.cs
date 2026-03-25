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

    public async Task OnBid(AuctionHistory auctionHistory)
    {
        await _hubContext.Clients.All.SendAsync("BidHistory",
            auctionHistory.Bids.Select(b => new BidDto((int)b.Seat, b.Bid.ToString(), b.IsAlerted)).ToList());
    }
    
}