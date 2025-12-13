using System.Diagnostics.Contracts;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class SeatRoleConstraint : IBidConstraint
{
    public SeatRoleType SeatRole;
    
    public SeatRoleConstraint(string type)
    {
        SeatRole = type.ToSeatRole();
    }
    public bool IsMet(DecisionContext ctx)
    {
        return ctx.AuctionEvaluation.SeatRoleType == SeatRole;
    }
}