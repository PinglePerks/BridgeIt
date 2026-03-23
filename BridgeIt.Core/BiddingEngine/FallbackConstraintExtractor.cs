using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine;

/// <summary>
/// Produces coarse <see cref="BidInformation"/> for bids that no bidding rule
/// could backward-match. This preserves partial knowledge from every bid in
/// the auction even when the specific convention is unknown.
///
/// Inferences made here are intentionally conservative — they represent the
/// weakest guarantees we can make purely from the bid's type and level,
/// without any convention-specific knowledge.
/// </summary>
public static class FallbackConstraintExtractor
{
    /// <summary>
    /// HCP minimums implied by opening at each level, used when no rule matches.
    /// Level 1 = 6 (could be a light opener or an overcall), Level 2+ = 6 (could be pre-emptive).
    /// These are intentionally low to avoid ruling out valid hands.
    /// </summary>
    private static readonly int[] SuitBidHcpMinByLevel = { 0, 6, 6, 10, 14, 16, 18 };

    private static readonly int[] NtBidHcpMinByLevel = { 0, 10, 18, 22, 25, 28, 33 };

    /// <summary>
    /// Minimum card length in the bid suit implied by a suit bid at each level
    /// when no specific rule matched.
    /// </summary>
    private static readonly int[] SuitBidMinLengthByLevel = { 0, 3, 5, 6, 7, 7, 7 };

    /// <summary>
    /// Produces a fallback <see cref="BidInformation"/> for a bid that no rule
    /// could explain. Returns <c>null</c> for Pass (a pass with no matched rule
    /// conveys nothing reliable).
    /// </summary>
    public static BidInformation? Extract(Bid bid)
    {
        return bid.Type switch
        {
            BidType.Pass => null,
            BidType.Suit => ExtractFromSuitBid(bid),
            BidType.NoTrumps => ExtractFromNtBid(bid),
            BidType.Double => ExtractFromDouble(),
            BidType.Redouble => null,
            _ => null
        };
    }

    private static BidInformation ExtractFromSuitBid(Bid bid)
    {
        var level = Math.Clamp(bid.Level, 1, 6);
        var hcpMin = SuitBidHcpMinByLevel[level];
        var minLength = SuitBidMinLengthByLevel[level];

        var constraint = new CompositeConstraint();
        constraint.Add(new HcpConstraint(hcpMin, 40));

        if (bid.Suit.HasValue)
            constraint.Add(new SuitLengthConstraint(bid.Suit.Value, minLength, 13));

        return new BidInformation(bid, constraint, PartnershipBiddingState.Unknown);
    }

    private static BidInformation ExtractFromNtBid(Bid bid)
    {
        var level = Math.Clamp(bid.Level, 1, 6);
        var hcpMin = NtBidHcpMinByLevel[level];

        var constraint = new CompositeConstraint();
        constraint.Add(new HcpConstraint(hcpMin, 40));
        constraint.Add(new BalancedConstraint());

        return new BidInformation(bid, constraint, PartnershipBiddingState.Unknown);
    }

    private static BidInformation ExtractFromDouble()
    {
        // A takeout or penalty double generally implies some values.
        var constraint = new HcpConstraint(8, 40);
        return new BidInformation(Bid.Double(), constraint, PartnershipBiddingState.Unknown);
    }
}
