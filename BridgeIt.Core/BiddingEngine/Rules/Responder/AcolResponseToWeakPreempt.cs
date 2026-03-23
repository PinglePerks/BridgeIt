using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder;

/// <summary>
/// Responses to partner's weak preempt opening (2-level suit through 4-level suit,
/// excluding 2C which is reserved for strong openings).
///
/// Decision tree (in priority order):
/// 1. Raise to game in a major — strong hand (14+ HCP, 3+ fit) OR distributional (4+ fit, 8+ HCP)
/// 2. Bid 3NT — 15+ HCP, stoppers in all unbid suits, short in partner's suit
/// 3. Extend the preempt — 3+ support, weak hand (under 10 HCP), raise one level
/// 4. Pass — default
/// </summary>
public class AcolResponseToWeakPreempt : BiddingRuleBase
{
    public override string Name { get; } = "Acol response to weak preempt";
    public override int Priority { get; } = 30;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 1)
            return false;

        var opening = auction.OpeningBid;
        if (opening == null || opening.Type != BidType.Suit || opening.Level < 2)
            return false;

        // 2C is strong, not a preempt
        if (opening == Bid.SuitBid(2, Suit.Clubs))
            return false;

        return true;
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // This rule always applies when the auction matches — it handles Pass too
        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.OpeningBid!;
        var partnerSuit = partnerBid.Suit!.Value;
        int hcp = ctx.HandEvaluation.Hcp;
        int support = ctx.HandEvaluation.Shape[partnerSuit];
        bool isMajor = partnerSuit == Suit.Hearts || partnerSuit == Suit.Spades;

        // 1. Raise to game in a major
        if (isMajor && partnerBid.Level < 4)
        {
            // Strong hand with fit
            if (hcp >= 14 && support >= 3)
                return Bid.SuitBid(4, partnerSuit);

            // Distributional hand with good fit
            if (hcp >= 8 && support >= 4)
                return Bid.SuitBid(4, partnerSuit);
        }

        // 2. Bid 3NT — to play, running partner's suit
        if (hcp >= 15 && partnerBid.Level <= 3)
        {
            if (HasStoppersInUnbidSuits(ctx.HandEvaluation.SuitStoppers, partnerSuit) && support <= 2)
                return Bid.NoTrumpsBid(3);
        }

        // 3. Extend the preempt — further barrage with support but weak hand
        if (support >= 3 && hcp < 10 && partnerBid.Level <= 3)
        {
            var raiseLevel = partnerBid.Level + 1;
            if (raiseLevel <= 5) // Don't bid beyond 5-level
                return Bid.SuitBid(raiseLevel, partnerSuit);
        }

        // 4. Default: pass
        return Bid.Pass();
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.OpeningBid!;
        var partnerSuit = partnerBid.Suit!.Value;
        bool isMajor = partnerSuit == Suit.Hearts || partnerSuit == Suit.Spades;

        // Pass is always explainable
        if (bid.Type == BidType.Pass) return true;

        // Game raise in partner's major
        if (isMajor && bid.Level == 4 && bid.Suit == partnerSuit)
            return true;

        // 3NT
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
            return true;

        // Preempt extension (one level above partner)
        if (bid.Suit == partnerSuit && bid.Level == partnerBid.Level + 1)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.OpeningBid!;
        var partnerSuit = partnerBid.Suit!.Value;
        bool isMajor = partnerSuit == Suit.Hearts || partnerSuit == Suit.Spades;

        // Pass — weak hand, insufficient support or values for action
        if (bid.Type == BidType.Pass)
        {
            return new BidInformation(bid, new HcpConstraint(0, 40), PartnershipBiddingState.SignOff);
        }

        // Game raise in partner's major
        if (isMajor && bid.Level == 4 && bid.Suit == partnerSuit)
        {
            var constraints = new CompositeConstraint();
            constraints.Add(new HcpConstraint(8, 40));
            constraints.Add(new SuitLengthConstraint(partnerSuit, 3, 13));
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        // 3NT — strong balanced hand
        if (bid.Type == BidType.NoTrumps && bid.Level == 3)
        {
            return new BidInformation(bid, new HcpConstraint(15, 40), PartnershipBiddingState.SignOff);
        }

        // Preempt extension — weak with support
        if (bid.Suit == partnerSuit && bid.Level == partnerBid.Level + 1)
        {
            var constraints = new CompositeConstraint();
            constraints.Add(new HcpConstraint(0, 9));
            constraints.Add(new SuitLengthConstraint(partnerSuit, 3, 13));
            return new BidInformation(bid, constraints, PartnershipBiddingState.SignOff);
        }

        return null;
    }

    private static bool HasStoppersInUnbidSuits(Dictionary<Suit, bool> stoppers, Suit partnerSuit)
    {
        foreach (Suit s in Enum.GetValues(typeof(Suit)))
        {
            if (s == partnerSuit)
                continue;

            if (!stoppers.TryGetValue(s, out var hasStopper) || !hasStopper)
                return false;
        }

        return true;
    }
}
