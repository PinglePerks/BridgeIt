using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: bid game in a confirmed fit suit.
/// Fires when combined HCP says BidGame and we have a confirmed 8+ card fit.
/// Major game = 4M, minor game = 5m.
/// </summary>
public class KnowledgeBidGameInSuit : BiddingRuleBase
{
    public override string Name => "Knowledge: Bid game in suit";
    public override int Priority { get; }

    public KnowledgeBidGameInSuit(int priority = 2)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // Only act on knowledge when partner has actively communicated via a bid
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == null) return false;

        var fitSuit = FindBestGameSuit(ctx);
        if (fitSuit == null) return false;

        var threshold = IsMajor(fitSuit.Value) ? 25 : 29;
        return ctx.GetLevelVerdict(threshold) == LevelVerdict.BidGame;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var fitSuit = FindBestGameSuit(ctx);
        if (fitSuit == null) return null;

        var gameLevel = IsMajor(fitSuit.Value) ? 4 : 5;
        var bid = Bid.SuitBid(gameLevel, fitSuit.Value);

        // Don't bid game if it's not legal (already past that level)
        if (ctx.AuctionEvaluation.CurrentContract != null &&
            !IsHigherBid(bid, ctx.AuctionEvaluation.CurrentContract))
            return null;

        return bid;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit) return false;
        if (bid.Level != 4 && bid.Level != 5) return false;
        if (bid.Level == 4 && !IsMajor(bid.Suit!.Value)) return false;
        if (bid.Level == 5 && IsMajor(bid.Suit!.Value)) return false;
        return true;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        // We can't give precise constraints since this is knowledge-derived
        // but we know it implies game values
        return new BidInformation(bid, null, PartnershipBiddingState.SignOff);
    }

    /// <summary>
    /// Find the best suit for game — prefer majors over minors, longest fit first.
    /// </summary>
    private static Suit? FindBestGameSuit(DecisionContext ctx)
    {
        Suit? bestMajor = null;
        Suit? bestMinor = null;
        int bestMajorLength = 0;
        int bestMinorLength = 0;

        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            if (!ctx.HasFitInSuit(suit)) continue;

            var myLength = ctx.HandEvaluation.Shape[suit];
            if (IsMajor(suit) && myLength > bestMajorLength)
            {
                bestMajor = suit;
                bestMajorLength = myLength;
            }
            else if (!IsMajor(suit) && myLength > bestMinorLength)
            {
                bestMinor = suit;
                bestMinorLength = myLength;
            }
        }

        // Prefer major game (4-level) over minor game (5-level)
        if (bestMajor != null && ctx.GetLevelVerdict(25) == LevelVerdict.BidGame)
            return bestMajor;

        if (bestMinor != null && ctx.GetLevelVerdict(29) == LevelVerdict.BidGame)
            return bestMinor;

        // If we have major game values but only a minor fit, still return it
        return bestMajor ?? bestMinor;
    }

    private static bool IsMajor(Suit suit) => suit == Suit.Hearts || suit == Suit.Spades;

    private static bool IsHigherBid(Bid newBid, Bid current)
    {
        if (newBid.Level > current.Level) return true;
        if (newBid.Level == current.Level)
        {
            if (newBid.Type == BidType.NoTrumps && current.Type == BidType.Suit) return true;
            if (newBid.Suit > current.Suit) return true;
        }
        return false;
    }
}
