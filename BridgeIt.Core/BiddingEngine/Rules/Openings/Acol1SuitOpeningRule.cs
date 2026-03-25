using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Openings;

public class Acol1SuitOpeningRule : BiddingRuleBase
{
    public override string Name { get; } = "Acol 1-Level Suit Opening";
    public override int Priority { get; }

    private readonly int _minHcp;
    private readonly int _maxHcp;
    private readonly int _minSuitLength;

    public Acol1SuitOpeningRule(int minHcp = 12, int maxHcp = 19, int minSuitLength = 4, int priority = 10)
    {
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        _minSuitLength = minSuitLength;
        Priority = priority;
    }

    // Forward: the full conjunction of what must be true for this rule to fire.
    // We can't include a suit constraint here because we don't know which suit yet.
    private CompositeConstraint BuildConstraints()
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp) } };

    // Backward: once we know which suit was bid, we can add the suit length.
    private CompositeConstraint BuildConstraints(Suit? suit)
        => new() { Constraints = { new HcpConstraint(_minHcp, _maxHcp), new SuitLengthConstraint(suit.ToString()!, $">={_minSuitLength}") } };

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => BuildConstraints();

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.NoBids;

    protected override bool IsHandApplicable(DecisionContext ctx)
        => ctx.HandEvaluation.Hcp >= _minHcp && ctx.HandEvaluation.Hcp <= _maxHcp;

    public override Bid? Apply(DecisionContext ctx)
    {
        var fiveCardSuits = ctx.HandEvaluation.SuitsWithMinLength(5);

        // Two 5+ card suits: bid the higher-ranking first
        if (fiveCardSuits.Count >= 2)
            return Bid.SuitBid(1, fiveCardSuits.First());

        // One 5+ card suit: bid it (even if it's a minor — longest suit first)
        if (fiveCardSuits.Count == 1)
            return Bid.SuitBid(1, fiveCardSuits.First());

        // All 4-card suits: bid the longest (highest-ranking breaks ties)
        return Bid.SuitBid(1, ctx.HandEvaluation.LongestAndStrongest);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Suit && bid.Level == 1;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
        => new(bid, BuildConstraints(bid.Suit), PartnershipBiddingState.ConstructiveSearch);
}