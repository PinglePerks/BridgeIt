using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive.Advancer;

/// <summary>
/// Advancing (responding to) partner's takeout double.
/// Partner doubled → advancer MUST bid (unless RHO intervened).
///
/// Four tiers:
///   0-8 HCP: bid best unbid suit at minimum level (forced response).
///   9-11 HCP: jump in best suit (invitational).
///   12+ HCP: cue bid opponent's suit (game-forcing).
///   6-10 HCP + stopper + balanced: bid NT.
/// </summary>
public class AdvanceAfterTakeoutDoubleRule : BiddingRuleBase
{
    public override string Name => "Advance After Takeout Double";
    public override int Priority { get; }

    public AdvanceAfterTakeoutDoubleRule(int priority = 8)
    {
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
        => auction.SeatRoleType == SeatRoleType.Overcaller
           && auction.MyLastNonPassBid == null
           && auction.PartnerLastNonPassBid is { Type: BidType.Double };

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        // Always applicable when partner doubled — forced to bid
        return true;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var hcp = ctx.HandEvaluation.Hcp;
        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;

        // 12+ HCP: cue bid opponent's suit (game-forcing)
        if (hcp >= 12 && opponentSuits.Count > 0)
        {
            var oppSuit = opponentSuits[0];
            var level = GetNextSuitBidLevel(oppSuit, currentContract);
            if (level <= 4) return Bid.SuitBid(level, oppSuit);
        }

        // 6-10 HCP + stopper + balanced: bid NT
        if (hcp >= 6 && hcp <= 10 && ctx.HandEvaluation.IsBalanced && HasStopperInOpponentSuit(ctx))
        {
            var ntLevel = GetNextNtBidLevel(currentContract);
            if (ntLevel <= 2) return Bid.NoTrumpsBid(ntLevel);
        }

        // Find best unbid suit
        var bestSuit = FindBestUnbidSuit(ctx);
        if (bestSuit == null) return Bid.Pass(); // Shouldn't happen in practice

        var cheapestLevel = GetNextSuitBidLevel(bestSuit.Value, currentContract);

        // 9-11 HCP: jump in best suit (invitational)
        if (hcp >= 9 && hcp <= 11)
        {
            var jumpLevel = cheapestLevel + 1;
            return jumpLevel <= 4 ? Bid.SuitBid(jumpLevel, bestSuit.Value) : Bid.SuitBid(cheapestLevel, bestSuit.Value);
        }

        // 0-8 HCP: bid at minimum level
        return cheapestLevel <= 4 ? Bid.SuitBid(cheapestLevel, bestSuit.Value) : Bid.Pass();
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx) => true; // Can explain any bid

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        var currentContract = ctx.AuctionEvaluation.CurrentContract;

        // Cue bid of opponent's suit = 12+ HCP
        if (bid.Type == BidType.Suit && bid.Suit.HasValue && opponentSuits.Contains(bid.Suit.Value))
        {
            return new BidInformation(bid, new CompositeConstraint
            {
                Constraints = { new HcpConstraint(12, 40) }
            }, PartnershipBiddingState.ConstructiveSearch);
        }

        // NT = 6-10 HCP, balanced
        if (bid.Type == BidType.NoTrumps)
        {
            var constraints = new CompositeConstraint
            {
                Constraints = { new HcpConstraint(6, 10), new BalancedConstraint() }
            };
            if (opponentSuits.Count > 0)
                constraints.Add(new StopperConstraint(opponentSuits[0]));
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }

        // Suit bid — check if it's a jump
        if (bid.Type == BidType.Suit && bid.Suit.HasValue)
        {
            var cheapestLevel = GetNextSuitBidLevel(bid.Suit.Value, currentContract);
            if (bid.Level > cheapestLevel)
            {
                // Jump = 9-11 HCP
                return new BidInformation(bid, new CompositeConstraint
                {
                    Constraints = { new HcpConstraint(9, 11), new SuitLengthConstraint(bid.Suit.Value, 4, 13) }
                }, PartnershipBiddingState.ConstructiveSearch);
            }
            else
            {
                // Minimum = 0-8 HCP
                return new BidInformation(bid, new CompositeConstraint
                {
                    Constraints = { new HcpConstraint(0, 8) }
                }, PartnershipBiddingState.ConstructiveSearch);
            }
        }

        return null;
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction) => null;

    private Suit? FindBestUnbidSuit(DecisionContext ctx)
    {
        var unbidSuits = ctx.AuctionEvaluation.UnbidSuits;
        if (unbidSuits.Count == 0) return null;

        // Pick the longest suit among unbid suits, prefer majors
        return unbidSuits
            .OrderByDescending(s => ctx.HandEvaluation.Shape.GetValueOrDefault(s, 0))
            .ThenByDescending(s => s) // Higher rank preferred
            .First();
    }

    private static bool HasStopperInOpponentSuit(DecisionContext ctx)
    {
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;
        if (opponentSuits.Count == 0) return true;
        return opponentSuits.All(suit =>
            ctx.HandEvaluation.SuitStoppers.TryGetValue(suit, out var quality)
            && quality >= StopperQuality.Full);
    }
}
