using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

/// <summary>
/// 1NT overcall — balanced hand with stopper in opponent's suit.
/// Direct seat: typically 15-17 or 16-18 HCP.
/// Protective seat: lower range, typically 11-14 HCP.
/// </summary>
public class NTOvercallRule : BiddingRuleBase
{
    private readonly int _directMinHcp;
    private readonly int _directMaxHcp;
    private readonly int _protectiveMinHcp;
    private readonly int _protectiveMaxHcp;

    public override string Name => "1NT Overcall";
    public override int Priority { get; }

    public NTOvercallRule(int directMinHcp = 15, int directMaxHcp = 17,
        int protectiveMinHcp = 12, int protectiveMaxHcp = 14, int priority = 16)
    {
        _directMinHcp = directMinHcp;
        _directMaxHcp = directMaxHcp;
        _protectiveMinHcp = protectiveMinHcp;
        _protectiveMaxHcp = protectiveMaxHcp;
        Priority = priority;
    }

    /// <summary>
    /// Forward constraints for negative inference — use the broadest range (direct).
    /// Stopper is not included because we can't know the opponent's suit generically.
    /// </summary>
    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new()
        {
            Constraints =
            {
                new HcpConstraint(_directMinHcp, _directMaxHcp),
                new BalancedConstraint()
            }
        };

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.BiddingRound == 1
           && auction.CurrentContract is { Type: BidType.Suit }  // Only over suit openings (not over NT)
           && auction.CurrentContract.Level == 1;                 // Only over 1-level openings

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (!ctx.HandEvaluation.IsBalanced) return false;

        var hcp = ctx.HandEvaluation.Hcp;
        var isProtective = ctx.AuctionEvaluation.IsProtectiveSeat;

        var minHcp = isProtective ? _protectiveMinHcp : _directMinHcp;
        var maxHcp = isProtective ? _protectiveMaxHcp : _directMaxHcp;

        if (hcp < minHcp || hcp > maxHcp) return false;

        // Must have stopper in opponent's suit
        return HasStopperInOpponentSuit(ctx);
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        return Bid.NoTrumpsBid(1);
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.NoTrumps && bid.Level == 1;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.NoTrumps || bid.Level != 1) return null;

        var constraints = new CompositeConstraint
        {
            Constraints =
            {
                new BalancedConstraint()
            }
        };

        // Add stopper constraint for the opponent's suit if known
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count > 0)
        {
            constraints.Add(new StopperConstraint(opponentSuits[0]));
        }

        // Use the broader range for backward inference — could be direct or protective
        constraints.Add(new HcpConstraint(
            Math.Min(_directMinHcp, _protectiveMinHcp),
            Math.Max(_directMaxHcp, _protectiveMaxHcp)));

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    private static bool HasStopperInOpponentSuit(DecisionContext ctx)
    {
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count == 0) return true; // No opponent suit known

        return opponentSuits.All(suit =>
            ctx.HandEvaluation.SuitStoppers.TryGetValue(suit, out var quality)
            && quality >= StopperQuality.Full);
    }
}
