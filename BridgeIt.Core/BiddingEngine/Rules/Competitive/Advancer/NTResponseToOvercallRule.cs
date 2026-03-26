using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;

/// <summary>
/// 1NT response to partner's overcall.
/// 8-11 HCP, balanced or semi-balanced, stopper in opponent's suit, no fit with partner.
/// </summary>
public class NTResponseToOvercallRule : BiddingRuleBase
{
    public override string Name => "NT Response to Overcall";
    public override int Priority { get; }

    public NTResponseToOvercallRule(int priority = 9)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.MyLastNonPassBid == null
           && auction.PartnerLastNonPassBid is { Type: BidType.Suit };

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        if (hcp < 8 || hcp > 11) return false;

        if (!ctx.HandEvaluation.IsBalanced) return false;

        // No fit with partner (less than 3 cards in partner's suit)
        var partnerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        if (ctx.HandEvaluation.Shape[partnerSuit] >= 3) return false;

        // Must have stopper in opponent's suit
        return HasStopperInOpponentSuit(ctx);
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var level = GetNextNtBidLevel(ctx.AuctionEvaluation.CurrentContract);
        return level <= 2 ? Bid.NoTrumpsBid(level) : null;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
        => bid.Type == BidType.NoTrumps;

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.NoTrumps) return null;

        var constraints = new CompositeConstraint
        {
            Constraints =
            {
                new HcpConstraint(8, 11),
                new BalancedConstraint()
            }
        };

        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count > 0)
        {
            constraints.Add(new StopperConstraint(opponentSuits[0]));
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction) => null;

    private static bool HasStopperInOpponentSuit(DecisionContext ctx)
    {
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count == 0) return true;

        return opponentSuits.All(suit =>
            ctx.HandEvaluation.SuitStoppers.TryGetValue(suit, out var quality)
            && quality >= StopperQuality.Full);
    }
}
