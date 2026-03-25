using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Rules.Knowledge;

/// <summary>
/// Knowledge-based: pass when no improvement is possible.
/// Fires at the lowest priority as the ultimate catch-all. Conditions:
/// - Verdict is SignOff, OR
/// - Current contract is already in our fit suit, OR
/// - No fit exists and we don't have game values
///
/// This replaces the engine's default "no rule matched → pass" with an
/// explicit rule, enabling backward inference to extract knowledge from it.
/// </summary>
public class KnowledgeSignOff : BiddingRuleBase
{
    public override string Name => "Knowledge: Sign off (pass)";
    public override int Priority { get; } = 0; // Absolute lowest — true catch-all

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.AuctionPhase != AuctionPhase.PreOpening;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // This is the catch-all — if we reach here, nothing better was found
        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.Pass();
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Pass;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, null, PartnershipBiddingState.SignOff);
}
