using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Players;

public interface IPlayer
{
    Task<Bid> GetBidAsync(BiddingContext context);
}

public class RobotPlayer(BiddingEngine.Core.BiddingEngine engine) : IPlayer
{
    public async Task<Bid> GetBidAsync(BiddingContext context)
    {
        return engine.ChooseBid(context);
    }
}

