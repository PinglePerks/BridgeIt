using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: sign off by raising a confirmed fit to the cheapest available level.
/// Fires when verdict is SignOff but a fit exists — places the contract in the best
/// part-score. E.g. after a transfer completion, responder with a weak hand passes
/// or corrects to the fit suit at the cheapest level.
/// </summary>
public class KnowledgeSignOffInFit : BiddingRuleBase
{
    public override string Name => "Knowledge: Sign off in fit";
    public override int Priority { get; }

    public KnowledgeSignOffInFit(int priority = 1)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // Only act on knowledge when partner has actively communicated via a bid
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == null) return false;
        
        var fitSuit = FindBestFitSuit(ctx);
        if (fitSuit == null) return false;
        
        var threshold = IsMajor(fitSuit.Value) ? 25 : 29;
        
        if (ctx.GetLevelVerdict(threshold) != LevelVerdict.SignOff) return false;
        
        // Check: is the current contract already in our fit suit?
        // If so, just pass — no need to raise
        var current = ctx.AuctionEvaluation.CurrentContract;
        if (current != null && current.Type == BidType.Suit && current.Suit == fitSuit)
            return false; // KnowledgeSignOff (pass) handles this

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var fitSuit = FindBestFitSuit(ctx);
        if (fitSuit == null) return null;

        var current = ctx.AuctionEvaluation.CurrentContract;
        var level = Bid.NextLevelForSuit(fitSuit.Value, current);

        // Don't push to game level when signing off
        var gameLevel = IsMajor(fitSuit.Value) ? 4 : 5;
        if (level >= gameLevel)
            return null; // Too high — just pass instead

        return Bid.SuitBid(level, fitSuit.Value);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit && bid.Level <= 3;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.SignOff);

    private static Suit? FindBestFitSuit(DecisionContext ctx)
    {
        // Prefer majors, then longest fit
        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            if (ctx.HasFitInSuit(suit)) return suit;
        }
        return null;
    }

    private static bool IsMajor(Suit suit) => suit == Suit.Hearts || suit == Suit.Spades;
}
