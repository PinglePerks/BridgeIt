using System.Diagnostics.Contracts;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class SeatRoleConstraint : IBidConstraint
{
    public SeatRole SeatRole;
    
    public SeatRoleConstraint(string type)
    {
        SeatRole = type.ParseSeatRole();
    }
    public bool IsMet(BiddingContext ctx)
    {
        return ctx.AuctionEvaluation.SeatRole == SeatRole;
    }
}