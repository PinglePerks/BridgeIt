using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;

/// <summary>
/// New suit over partner's overcall — constructive, non-forcing.
/// 5+ card suit, 8+ HCP, bid at cheapest level.
/// </summary>
public class NewSuitOverOvercallRule : BiddingRuleBase
{
    public override string Name => "New Suit Over Overcall";
    public override int Priority { get; }

    public NewSuitOverOvercallRule(int priority = 10)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.MyLastNonPassBid == null           // Advancer
           && auction.PartnerLastNonPassBid is { Type: BidType.Suit }; // Partner overcalled

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < 8) return false;

        return FindNewSuit(ctx) != null;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = FindNewSuit(ctx);
        if (suit == null) return null;

        var level = GetNextSuitBidLevel(suit.Value, ctx.AuctionEvaluation.CurrentContract);
        return level <= 3 ? Bid.SuitBid(level, suit.Value) : null;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return false;
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid?.Suit;
        return bid.Suit != partnerSuit; // Must be a different suit from partner's
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return null;

        var constraints = new CompositeConstraint
        {
            Constraints =
            {
                new HcpConstraint(8, 40),
                new SuitLengthConstraint(bid.Suit.Value, 5, 13)
            }
        };

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction) => null;

    private Suit? FindNewSuit(DecisionContext ctx)
    {
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        var candidates = ctx.HandEvaluation.SuitsWithMinLength(5);

        foreach (var suit in candidates)
        {
            if (suit == partnerSuit) continue; // Must be a new suit
            var level = GetNextSuitBidLevel(suit, currentContract);
            if (level <= 3) return suit;
        }

        return null;
    }
}
