using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponderRebids;

/// <summary>
/// Responder's continuation after 2♣ – 2♦ – [suit rebid].
///
/// Sequence: 2♣ (opener) – 2♦ (responder, artificial) – 2♥/2♠/3♣/3♦ (opener, natural) – ?
///
/// The auction is game-forcing. Responder must not pass below game.
///
/// Decision order:
///   1. 3+ support for opener's suit → simple raise (any strength)
///   2. 5+ card suit of own → bid cheapest level (spades first)
///   3. Otherwise → bid cheapest NT
///
/// Backward inference uses shape + basic HCP tiers (0-7 weak, 8+ values).
/// </summary>
public class AcolResponderAfter2CSuitRebid : BiddingRuleBase
{
    public override string Name => "Acol responder after 2C suit rebid";
    public override int Priority { get; }

    public AcolResponderAfter2CSuitRebid(int priority = 55)
    {
        Priority = priority;
    }

    private const int WeakMax = 7;

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        if (auction.SeatRoleType != SeatRoleType.Responder || auction.BiddingRound != 2)
            return false;
        if (auction.AuctionPhase != AuctionPhase.Uncontested)
            return false;
        if (auction.OpeningBid != Bid.SuitBid(2, Suit.Clubs))
            return false;
        if (auction.MyLastNonPassBid != Bid.SuitBid(2, Suit.Diamonds))
            return false;

        // Opener's rebid must be a suit (not NT — those are handled by Stayman/Transfer rules)
        var partnerBid = auction.PartnerLastNonPassBid;
        return partnerBid is { Type: BidType.Suit };
    }

    protected override bool IsHandApplicable(DecisionContext ctx) => true;

    public override Bid? Apply(DecisionContext ctx)
    {
        var openerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var shape = ctx.HandEvaluation.Shape;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        // 1. Fit: 3+ support → raise opener's suit
        if (shape[openerSuit] >= 3)
        {
            var level = GetNextSuitBidLevel(openerSuit, contract);
            return Bid.SuitBid(level, openerSuit);
        }

        // 2. No fit: show own 5+ card suit (highest ranking first)
        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            if (suit == openerSuit) continue;
            if (shape[suit] >= 5)
            {
                var level = GetNextSuitBidLevel(suit, contract);
                return Bid.SuitBid(level, suit);
            }
        }

        // 3. Fallback: bid cheapest NT
        var ntLevel = Bid.NextLevelForNoTrumps(contract);
        return Bid.NoTrumpsBid(ntLevel);
    }

    // ── Backward ────────────────────────────────────────────────────────────

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type == BidType.NoTrumps && bid.Level >= 2)
            return true;

        if (bid is { Type: BidType.Suit, Suit: not null } && bid.Level >= 2)
            return true;

        return false;
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        var openerSuit = ctx.AuctionEvaluation.PartnerLastNonPassBid!.Suit!.Value;
        var contract = ctx.AuctionEvaluation.CurrentContract;

        // Raise of opener's suit — shows 3+ support, any strength
        if (bid is { Type: BidType.Suit } && bid.Suit == openerSuit)
        {
            return new BidInformation(bid,
                new CompositeConstraint { Constraints = { new SuitLengthConstraint(openerSuit, 3, 13) } },
                PartnershipBiddingState.ConstructiveSearch);
        }

        // New suit — shows 5+ cards
        if (bid is { Type: BidType.Suit, Suit: not null })
        {
            var minLevel = GetNextSuitBidLevel(bid.Suit.Value, contract);
            var isJump = bid.Level > minLevel;

            var constraints = new CompositeConstraint
            {
                Constraints =
                {
                    new SuitLengthConstraint(bid.Suit.Value, 5, 13),
                    isJump ? new HcpConstraint(WeakMax + 1, 40) : new HcpConstraint(0, WeakMax)
                }
            };
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }

        // NT
        if (bid.Type == BidType.NoTrumps)
        {
            var minNtLevel = Bid.NextLevelForNoTrumps(contract);
            var isJump = bid.Level > minNtLevel;

            var constraints = new CompositeConstraint
            {
                Constraints =
                {
                    isJump ? new HcpConstraint(WeakMax + 1, 40) : new HcpConstraint(0, WeakMax)
                }
            };
            return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
        }

        return null;
    }

    // No meaningful forward constraint — responder's 2♦ showed 0+ HCP
    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction) => null;
}
