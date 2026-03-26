using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

/// <summary>
/// Simple overcall at the cheapest level in a 5+ card suit.
/// Direct seat: full HCP range (typically 8-15).
/// Protective seat: lower threshold, can stretch to 4-card suit.
///
/// Priority should be above JumpOvercall but below NTOvercall.
/// </summary>
public class SimpleOvercallRule : BiddingRuleBase
{
    private readonly int _minHcp;
    private readonly int _maxHcp;
    private readonly int _minSuitLength;

    public override string Name => "Simple Overcall";
    public override int Priority { get; }

    public SimpleOvercallRule(int minHcp = 8, int maxHcp = 15, int minSuitLength = 5, int priority = 15)
    {
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        _minSuitLength = minSuitLength;
        Priority = priority;
    }

    private CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint("any", $">={_minSuitLength}") } };

    private CompositeConstraint BuildConstraints(Suit suit)
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint(suit, _minSuitLength, 13) } };

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.BiddingRound == 1
           && auction.CurrentContract is { Type: BidType.Suit or BidType.NoTrumps };

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp < _minHcp || hcp > _maxHcp) return false;

        // In protective seat, allow 4-card suits
        var minLength = ctx.AuctionEvaluation.IsProtectiveSeat ? _minSuitLength - 1 : _minSuitLength;

        return FindBestSuit(ctx, minLength) != null;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var minLength = ctx.AuctionEvaluation.IsProtectiveSeat ? _minSuitLength - 1 : _minSuitLength;
        var suit = FindBestSuit(ctx, minLength);
        if (suit == null) return null;

        var level = GetNextSuitBidLevel(suit.Value, ctx.AuctionEvaluation.CurrentContract);
        return level <= 3 ? Bid.SuitBid(level, suit.Value) : null; // Don't overcall at 4-level with minimum
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit) return false;
        // Simple overcall = bid at cheapest possible level for this suit
        var cheapestLevel = GetNextSuitBidLevel(bid.Suit!.Value, ctx.AuctionEvaluation.CurrentContract);
        return bid.Level == cheapestLevel;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return null;
        return new BidInformation(bid, BuildConstraints(bid.Suit.Value), PartnershipBiddingState.ConstructiveSearch);
    }

    /// <summary>
    /// Find the longest suit with at least minLength cards that can be bid above the current contract.
    /// Prefers longer suits, then higher-ranking suits.
    /// </summary>
    private Suit? FindBestSuit(DecisionContext ctx, int minLength)
    {
        var candidates = ctx.HandEvaluation.SuitsWithMinLength(minLength);
        var currentContract = ctx.AuctionEvaluation.CurrentContract;

        foreach (var suit in candidates)
        {
            var level = GetNextSuitBidLevel(suit, currentContract);
            if (level <= 3) return suit; // Can bid at a reasonable level
        }

        return null;
    }
}
