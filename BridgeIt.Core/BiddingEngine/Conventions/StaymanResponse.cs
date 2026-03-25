using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Opener's response to Stayman — show a 4-card major or deny with diamonds.
/// Parameterised by NTConventionContext so the same class works after 1NT, 2NT, or 2C-2D-2NT.
/// </summary>
public class StaymanResponse : BiddingRuleBase
{
    private readonly NTConventionContext _ntCtx;

    public StaymanResponse(NTConventionContext ntCtx, int priority = 60)
    {
        _ntCtx = ntCtx;
        Priority = priority;
    }

    public override string Name => $"Stayman response after {_ntCtx.Name}";
    public override int Priority { get; }
    public override bool IsAlertable => true;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener) return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested) return false;

        // My last bid was the NT-showing bid, partner bid Stayman
        if (auction.MyLastNonPassBid != Bid.NoTrumpsBid(_ntCtx.NTLevel)) return false;
        if (auction.PartnerLastBid != _ntCtx.StaymanBid) return false;
        if (auction.CurrentContract != _ntCtx.StaymanBid) return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var level = _ntCtx.ConventionLevel;

        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 4)
            return Bid.SuitBid(level, Suit.Hearts);

        if (ctx.HandEvaluation.Shape[Suit.Spades] >= 4)
            return Bid.SuitBid(level, Suit.Spades);

        return Bid.SuitBid(level, Suit.Diamonds); // deny both majors
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit
           && bid.Level == _ntCtx.ConventionLevel
           && bid.Suit != Suit.Clubs;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();

        if (bid.Suit == Suit.Hearts)
            constraints.Add(new SuitLengthConstraint(Suit.Hearts, 4, 5));

        if (bid.Suit == Suit.Spades)
        {
            constraints.Add(new SuitLengthConstraint(Suit.Spades, 4, 5));
            constraints.Add(new SuitLengthConstraint(Suit.Hearts, 2, 3)); // denied hearts
        }

        if (bid.Suit == Suit.Diamonds)
        {
            constraints.Add(new SuitLengthConstraint(Suit.Hearts, 2, 3));
            constraints.Add(new SuitLengthConstraint(Suit.Spades, 2, 3));
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }
}
