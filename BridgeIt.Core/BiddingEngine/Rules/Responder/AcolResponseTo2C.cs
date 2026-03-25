using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder;

public class AcolResponseTo2C : BiddingRuleBase
{
    public override string Name { get; } = "Acol response to 2C";
    public override int Priority { get; }
    public override bool IsAlertable => true;

    public AcolResponseTo2C(int priority = 50)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 1)
            return false;

        if (auction.OpeningBid != Bid.SuitBid(2, Suit.Clubs))
            return false;
        
        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
        => true;
    public override Bid? Apply(DecisionContext ctx)
        => Bid.SuitBid(2, Suit.Diamonds);
    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid == Bid.SuitBid(2, Suit.Diamonds))
            return true;
        return false;
    }
    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        return new BidInformation(bid, null, PartnershipBiddingState.ConstructiveSearch);
    }
}