using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: bid 2NT as an invitation when combined HCP straddles game
/// but no confirmed major suit fit exists.
/// </summary>
public class KnowledgeInviteInNT : BiddingRuleBase
{
    public override string Name => "Knowledge: Invite in NT";
    public override int Priority { get; }

    public KnowledgeInviteInNT(int priority = 1)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.AuctionEvaluation.PartnerLastNonPassBid == null) return false;
        if (ctx.PartnershipBiddingState == PartnershipBiddingState.SignOff) return false;
        if (ctx.GetLevelVerdict(25) != LevelVerdict.Invite) return false;

        // No confirmed major fit
        if (ctx.HasFitInSuit(Suit.Spades) || ctx.HasFitInSuit(Suit.Hearts))
            return false;

        // 2NT must be a legal bid
        var current = ctx.AuctionEvaluation.CurrentContract;
        if (current != null && !IsHigherBid(Bid.NoTrumpsBid(2), current))
            return false;

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(2);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.NoTrumps && bid.Level == 2;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.GameInvitational);

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
