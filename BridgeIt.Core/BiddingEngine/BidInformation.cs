using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine;

public class BidInformation
{
    public Bid Bid { get; init; }
    
    public IBidConstraint? Constraint { get; init; }
    public string? PartnershipState { get; init; }

    public BidInformation(Bid bid, IBidConstraint? constraint, string? partnershipState)
    {
        Bid = bid;
        Constraint = constraint;
        PartnershipState = partnershipState;
    }
    
}