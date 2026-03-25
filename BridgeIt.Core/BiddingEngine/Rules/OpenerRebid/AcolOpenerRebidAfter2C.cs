using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolOpenerRebidAfter2C : BiddingRuleBase
{
    public override string Name { get; } = "Acol opener rebid after 2C";
    public override int Priority { get; } = 60;

    private const int MinHcp2NT = 23;
    private const int MaxHcp2NT = 24;
    private const int MinHcp3NT = 25;
    private const int MaxHcp3NT = 35;
    private const int MinHcpUnbalanced = 20;
    private const int MaxHcpUnbalanced = 35;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Opener || auction.BiddingRound != 2)
            return false;

        return auction.OpeningBid == Bid.SuitBid(2, Suit.Clubs)
               && auction.PartnerLastNonPassBid == Bid.SuitBid(2, Suit.Diamonds);
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp >= MinHcpUnbalanced;

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;

        if (ctx.HandEvaluation.IsBalanced)
        {
            if (hcp >= MinHcp3NT)
                return Bid.NoTrumpsBid(3);

            if (hcp >= MinHcp2NT)
                return Bid.NoTrumpsBid(2);
        }

        // Unbalanced: bid longest suit
        var suit = ctx.HandEvaluation.LongestAndStrongest;
        var nextLevel = GetNextSuitBidLevel(suit, ctx.AuctionEvaluation.CurrentContract);
        return Bid.SuitBid(nextLevel, suit);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.NoTrumps && bid.Level is 2 or 3)
            return true;

        if (bid.Type == BidType.Suit && bid.Level >= 2)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var constraints = new CompositeConstraint();

        if (bid.Type == BidType.NoTrumps)
        {
            constraints.Add(new BalancedConstraint());

            if (bid.Level == 2)
                constraints.Add(new HcpConstraint(MinHcp2NT, MaxHcp2NT));
            else
                constraints.Add(new HcpConstraint(MinHcp3NT, MaxHcp3NT));

            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }

        if (bid is { Type: BidType.Suit, Suit: not null })
        {
            constraints.Add(new HcpConstraint(MinHcpUnbalanced, MaxHcpUnbalanced));
            constraints.Add(new SuitLengthConstraint(bid.Suit.Value, 4, 13));
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }

        return null;
    }
}
