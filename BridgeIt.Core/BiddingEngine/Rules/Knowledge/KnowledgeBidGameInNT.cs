using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: bid 3NT when we have game values but no confirmed suit fit.
/// Fires when combined HCP says BidGame, hand is reasonably balanced,
/// and no major suit fit exists.
/// </summary>
public class KnowledgeBidGameInNT : BiddingRuleBase
{
    public override string Name => "Knowledge: Bid 3NT";
    public override int Priority { get; } = 1; // Below suit game — prefer suit fit

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (!ctx.TableKnowledge.Partner.HasMeaningfulKnowledge) return false;
        if (ctx.GetLevelVerdict(25) != LevelVerdict.BidGame) return false;

        // No confirmed major fit
        if (ctx.HasFitInSuit(Suit.Spades) || ctx.HasFitInSuit(Suit.Hearts))
            return false;

        // 3NT must be a legal bid
        var current = ctx.AuctionEvaluation.CurrentContract;
        if (current != null && !IsHigherBid(Bid.NoTrumpsBid(3), current))
            return false;

        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(3);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.NoTrumps && bid.Level == 3;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.SignOff);

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
