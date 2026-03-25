using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine;

public class BidInformation
{
    public Bid Bid { get; init; }
    
    public IBidConstraint? Constraint { get; init; }
    public PartnershipBiddingState PartnershipBiddingState { get; init; }

    public BidInformation(Bid bid, IBidConstraint? constraint, PartnershipBiddingState partnershipState)
    {
        Bid = bid;
        Constraint = constraint;
        PartnershipBiddingState = partnershipState;
    }
    
}