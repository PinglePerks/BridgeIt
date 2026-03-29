using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

/// <summary>
/// Jump overcall — one level above the cheapest available bid.
/// Two styles:
///   "Intermediate" — 12-16 HCP, strong 6+ card suit.
///   "Weak" — 6-10 HCP, pre-emptive 6+ card suit.
/// Style is set by JumpOvercallConfig in the system JSON.
/// </summary>
public class JumpOvercallRule : BiddingRuleBase
{
    private readonly string _style;
    private readonly int _minHcp;
    private readonly int _maxHcp;
    private readonly int _minSuitLength;

    public override string Name => $"Jump Overcall ({_style})";
    public override int Priority { get; }

    public JumpOvercallRule(string style = "Intermediate", int minHcp = 12, int maxHcp = 16,
        int minSuitLength = 6, int priority = 14)
    {
        _style = style;
        _minHcp = minHcp;
        _maxHcp = maxHcp;
        _minSuitLength = minSuitLength;
        Priority = priority;
    }

    private CompositeConstraint BuildConstraints()
        => new()
        {
            Constraints =
            {
                new HcpConstraint(_minHcp, _maxHcp),
                new SuitLengthConstraint("any", $">={_minSuitLength}")
            }
        };

    private CompositeConstraint BuildConstraints(Suit suit)
        => new()
        {
            Constraints =
            {
                new HcpConstraint(_minHcp, _maxHcp),
                new SuitLengthConstraint(suit, _minSuitLength, 13)
            }
        };

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

        return FindBestSuit(ctx) != null;
    }

    public override Bid? Apply(DecisionContext ctx)
    {
        var suit = FindBestSuit(ctx);
        if (suit == null) return null;

        var cheapestLevel = GetNextSuitBidLevel(suit.Value, ctx.AuctionEvaluation.CurrentContract);
        var jumpLevel = cheapestLevel + 1; // Jump = one level above cheapest
        return jumpLevel <= 4 ? Bid.SuitBid(jumpLevel, suit.Value) : null;
    }

    protected override bool IsBidExplainable(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return false;
        if (ctx.AuctionEvaluation.OpponentBidSuits.Contains(bid.Suit.Value)) return false;
        var cheapestLevel = GetNextSuitBidLevel(bid.Suit.Value, ctx.AuctionEvaluation.CurrentContract);
        return bid.Level == cheapestLevel + 1; // Exactly a single jump
    }

    public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
    {
        if (bid.Type != BidType.Suit || !bid.Suit.HasValue) return null;
        return new BidInformation(bid, BuildConstraints(bid.Suit.Value), PartnershipBiddingState.ConstructiveSearch);
    }

    private Suit? FindBestSuit(DecisionContext ctx)
    {
        var candidates = ctx.HandEvaluation.SuitsWithMinLength(_minSuitLength);
        var currentContract = ctx.AuctionEvaluation.CurrentContract;
        var opponentSuits = ctx.AuctionEvaluation.OpponentBidSuits;

        foreach (var suit in candidates)
        {
            if (opponentSuits.Contains(suit)) continue; // Never overcall in opponent's suit
            var cheapestLevel = GetNextSuitBidLevel(suit, currentContract);
            if (cheapestLevel + 1 <= 4) return suit;
        }

        return null;
    }
}
