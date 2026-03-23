using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;

public class AcolRebidNewSuit : BiddingRuleBase
{
    public override string Name { get; } = "Acol rebid new suit";
    public override int Priority { get; } = 40;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 2)
        {
            if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
                return true;
        }
        return false;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.IsBalanced)
            return false;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var shape = ctx.HandEvaluation.Shape;

        // 6+ card opening suit → RebidOwnSuit handles it
        if (shape[firstBidSuit] >= 6)
            return false;

        var secondSuit = PickSecondSuit(ctx);
        if (secondSuit == null)
            return false;

        // Reverse = second suit outranks first AND opening suit is not Clubs.
        // (1C-1S-2D is not a standard reverse because clubs is the lowest suit
        //  and partner can always return to opener's suit at the 2-level.)
        // A reverse requires 16+ HCP.
        bool isReverse = IsReverse(secondSuit.Value, firstBidSuit, ctx);
        if (isReverse && ctx.HandEvaluation.Hcp < 16)
            return false;

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var secondSuit = PickSecondSuit(ctx);
        if (secondSuit == null)
            return null;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var nextLevel = GetNextSuitBidLevel(secondSuit.Value, ctx.AuctionEvaluation.CurrentContract);
        bool isReverse = IsReverse(secondSuit.Value, firstBidSuit, ctx);

        // A reverse already promises 16+ HCP — do not additionally jump.
        // A jump shift (non-reverse, 16+ HCP at 2-level) shows extra values.
        if (!isReverse && nextLevel <= 2 && ctx.HandEvaluation.Hcp >= 16)
            return Bid.SuitBid(nextLevel + 1, secondSuit.Value);

        return Bid.SuitBid(nextLevel, secondSuit.Value);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit)
            return false;

        // Must be a different suit from opener's opening bid
        if (bid.Suit == ctx.AuctionEvaluation.OpeningBid!.Suit)
            return false;

        // Must not be partner's suit (raising partner's suit = RebidRaiseSuit)
        if (bid.Suit == ctx.AuctionEvaluation.PartnerLastBid?.Suit)
            return false;

        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Suit == null)
            return null;

        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit;
        var nextLevel = GetNextSuitBidLevel((Suit)bid.Suit, ctx.AuctionEvaluation.CurrentContract);

        bool isReverse = bid.Suit.HasValue && firstBidSuit.HasValue &&
                         IsReverse(bid.Suit.Value, firstBidSuit.Value, ctx);

        var constraints = new CompositeConstraint();
        // Second (new) suit: at least 4 cards — this is the primary shown suit
        constraints.Add(new SuitLengthConstraint(bid.Suit, 4, 5));
        // Opening suit: 4-5 cards (6+ would have triggered RebidOwnSuit)
        constraints.Add(new SuitLengthConstraint(firstBidSuit, 4, 5));

        // Reverse or jump shift = 16+ HCP; minimum new suit = 12-15 HCP
        if (isReverse || bid.Level > nextLevel)
            constraints.Add(new HcpConstraint(16, 19));
        else
            constraints.Add(new HcpConstraint(12, 15));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Picks opener's best second suit: a 4+ card suit that is not the opening suit
    /// and not partner's suit. At the 1-level, bids cheapest (up the line).
    /// At the 2-level, bids lower-ranking first (leaves room for the other suit).
    /// </summary>
    private Suit? PickSecondSuit(DecisionContext ctx)
    {
        var firstBidSuit = ctx.AuctionEvaluation.OpeningBid!.Suit!.Value;
        var partnerBidSuit = ctx.AuctionEvaluation.PartnerLastBid?.Suit;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        var candidates = ctx.HandEvaluation.SuitsWithMinLength(4)
            .Where(s => s != firstBidSuit && s != partnerBidSuit)
            .ToList();

        if (!candidates.Any())
            return null;

        // Prefer 1-level bids (cheapest rank = last in the descending list)
        var oneLevelCandidates = candidates
            .Where(s => GetNextSuitBidLevel(s, contract) == 1)
            .ToList();

        if (oneLevelCandidates.Any())
            return oneLevelCandidates.Last(); // SuitsWithMinLength orders high→low, Last = cheapest

        // At 2-level: bid lower-ranking suit first to leave room for the other
        var twoLevelCandidates = candidates
            .Where(s => GetNextSuitBidLevel(s, contract) >= 2)
            .OrderBy(s => (int)s)
            .ToList();

        return twoLevelCandidates.Any() ? twoLevelCandidates[0] : null;
    }

    /// <summary>
    /// A reverse occurs when the second suit is higher-ranking than opener's first suit
    /// AND the opening suit is not Clubs (the lowest suit).
    /// After a 1C opening, any 2-level second suit is not a classic reverse because
    /// partner can still show a club preference at the 2-level.
    /// </summary>
    private static bool IsReverse(Suit secondSuit, Suit openingSuit, DecisionContext ctx)
    {
        if (openingSuit == Suit.Clubs)
            return false;

        var nextLevel = Bid.NextLevelForSuit(secondSuit, ctx.AuctionEvaluation.CurrentContract);
        return (int)secondSuit > (int)openingSuit && nextLevel >= 2;
    }
}
