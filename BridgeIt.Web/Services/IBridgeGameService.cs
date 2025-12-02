using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Web.Services;

public interface IBridgeGameService
{
    // Start a new game with a random deal
    Task<Dictionary<Seat, Hand>> NewGameAsync();
    
    // Make a bid (for the human player)
    Task<BiddingDecision> HumanBidAsync(Bid bid);
    
    // Ask the engine to make a bid (for bots)
    Task<BiddingDecision> BotBidAsync(Seat seat);
    
    // Get current auction history
    List<BiddingDecision> GetAuctionHistory();
    
    // Get the dealer for the current board
    Seat GetDealer();
}