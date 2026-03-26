using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

/// <summary>
/// Takeout double — asks partner to bid their best suit.
///
/// Two qualifying paths:
///   1. Classic shape: MinHcp+, 0-2 cards in opponent's suit, 3+ in each unbid suit.
///   2. Strong override: StrongOverrideHcp+ regardless of shape (too strong to overcall).
///
/// Fires in both direct and protective seats over suit openings at level 1-3.
/// </summary>
public class TakeoutDoubleRule : BiddingRuleBase
{
    private readonly int _minHcp;
    private readonly int _strongOverrideHcp;

    public override string Name => "Takeout Double";
    public override int Priority { get; }

    public TakeoutDoubleRule(int minHcp = 12, int strongOverrideHcp = 16, int priority = 13)
    {
        _minHcp = minHcp;
        _strongOverrideHcp = strongOverrideHcp;
        Priority = priority;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
    {
        // For negative inference: just use the HCP floor (the classic path minimum)
        return new CompositeConstraint { Constraints = { new HcpConstraint(_minHcp, 40) } };
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.BiddingRound == 1
           && auction.CurrentContract is { Type: BidType.Suit }
           && auction.CurrentContract.Level <= 3;

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp < _minHcp) return false;

        // Path 2: strong override — any shape with 16+ HCP
        if (hcp >= _strongOverrideHcp) return true;

        // Path 1: classic shape — short in opponent's suit, support for unbid suits
        return HasClassicTakeoutShape(ctx);
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.Double();
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.Double;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Double) return null;

        // Backward inference: at minimum, HCP >= _minHcp
        var constraints = new CompositeConstraint
        {
            Constraints = { new HcpConstraint(_minHcp, 40) }
        };

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    private bool HasClassicTakeoutShape(DecisionContext ctx)
    {
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count == 0) return false;

        // Short in opponent's suit (0-2 cards)
        foreach (var oppSuit in opponentSuits)
        {
            if (ctx.HandEvaluation.Shape.TryGetValue(oppSuit, out var count) && count > 2)
                return false;
        }

        // Support for all unbid suits (3+ cards in each)
        var unbidSuits = ctx.AuctionEvaluation.UnbidSuits;
        foreach (var suit in unbidSuits)
        {
            if (!ctx.HandEvaluation.Shape.TryGetValue(suit, out var count) || count < 3)
                return false;
        }

        return true;
    }
}
