using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: invite game by raising a fit suit when verdict is Invite.
/// Raises the fit suit to one level below game, asking partner to bid on with maximum.
/// E.g. raise to 3M with a major fit and invitational values.
/// </summary>
public class KnowledgeInviteInSuit : BiddingRuleBase
{
    public override string Name => "Knowledge: Invite in suit";
    public override int Priority { get; } = 2;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (!ctx.TableKnowledge.Partner.HasMeaningfulKnowledge) return false;
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == null) return false;

        var fitSuit = FindBestInviteSuit(ctx);
        if (fitSuit == null) return false;

        var threshold = IsMajor(fitSuit.Value) ? 25 : 29;
        return ctx.GetLevelVerdict(threshold) == LevelVerdict.Invite;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var fitSuit = FindBestInviteSuit(ctx);
        if (fitSuit == null) return null;

        // Invite = one level below game
        var inviteLevel = IsMajor(fitSuit.Value) ? 3 : 4;
        var bid = Bid.SuitBid(inviteLevel, fitSuit.Value);

        var current = ctx.AuctionEvaluation.CurrentContract;
        if (current != null && !IsHigherBid(bid, current))
            return null;

        return bid;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit) return false;
        // 3-level major or 4-level minor could be invitational
        if (bid.Level == 3 && IsMajor(bid.Suit!.Value)) return true;
        if (bid.Level == 4 && !IsMajor(bid.Suit!.Value)) return true;
        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.GameInvitational);

    private static Suit? FindBestInviteSuit(DecisionContext ctx)
    {
        // Prefer confirmed fit, then possible fit; majors first
        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            if (ctx.HasFitInSuit(suit)) return suit;
        }
        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            if (ctx.HasPossibleFitInSuit(suit)) return suit;
        }
        return null;
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
