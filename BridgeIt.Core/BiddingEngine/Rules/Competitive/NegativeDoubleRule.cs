using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Rules.Competitive;

/// <summary>
/// Negative double — by responder when opponent overcalls partner's opening.
/// Shows 4+ in each unbid major, 6+ HCP.
/// Only applies when opponent's overcall is at or below the configured maxLevel.
/// </summary>
public class NegativeDoubleRule : BiddingRuleBase
{
    private readonly Bid? _maxLevelBid;
    private readonly int _minHcp;

    public override string Name => "Negative Double";
    public override int Priority { get; }

    public NegativeDoubleRule(string maxLevel = "2S", int minHcp = 6, int priority = 12)
    {
        _minHcp = minHcp;
        _maxLevelBid = ParseBidString(maxLevel);
        Priority = priority;
    }

    protected override bool IsApplicableContext(AuctionEvaluation auction)
    {
        // Must be responder (partner opened) in a contested auction
        if (auction.SeatRoleType != SeatRoleType.Responder) return false;
        if (auction.AuctionPhase != AuctionPhase.Contested) return false;
        if (auction.BiddingRound != 1) return false;

        // RHO must have overcalled with a suit bid at or below max level
        var rhoBid = auction.RhoLastNonPassBid;
        if (rhoBid == null || rhoBid.Type != BidType.Suit) return false;

        if (_maxLevelBid == null) return true;
        return IsBidAtOrBelow(rhoBid, _maxLevelBid);
    }

    protected override bool IsHandApplicable(DecisionContext ctx)
    {
        if (ctx.HandEvaluation.Hcp < _minHcp) return false;

        // Must have 4+ in each unbid major
        var unbidSuits = ctx.AuctionEvaluation.UnbidSuits;
        var unbidMajors = unbidSuits.Where(s => s == Suit.Hearts || s == Suit.Spades).ToList();

        if (unbidMajors.Count == 0) return false; // No unbid majors to show

        return unbidMajors.All(major => ctx.HandEvaluation.Shape[major] >= 4);
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

        var constraints = new CompositeConstraint
        {
            Constraints = { new HcpConstraint(_minHcp, 40) }
        };

        // Add 4+ in each unbid major
        var unbidSuits = ctx.AuctionEvaluation.UnbidSuits;
        foreach (var suit in unbidSuits.Where(s => s == Suit.Hearts || s == Suit.Spades))
        {
            constraints.Add(new SuitLengthConstraint(suit, 4, 13));
        }

        return new BidInformation(bid, constraints, PartnershipBiddingState.ConstructiveSearch);
    }

    public override CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction)
        => new() { Constraints = { new HcpConstraint(_minHcp, 40) } };

    public override bool IsAlertable => true;

    private static Bid? ParseBidString(string bidStr)
    {
        if (string.IsNullOrWhiteSpace(bidStr) || bidStr.Length < 2) return null;
        if (!int.TryParse(bidStr[..1], out var level)) return null;

        var suitChar = bidStr[1..].ToUpper();
        Suit? suit = suitChar switch
        {
            "C" => Suit.Clubs,
            "D" => Suit.Diamonds,
            "H" => Suit.Hearts,
            "S" => Suit.Spades,
            _ => null
        };

        return suit.HasValue ? Bid.SuitBid(level, suit.Value) : null;
    }

    private static bool IsBidAtOrBelow(Bid bid, Bid maxBid)
    {
        if (bid.Level < maxBid.Level) return true;
        if (bid.Level > maxBid.Level) return false;
        // Same level — compare suits
        return bid.Suit <= maxBid.Suit;
    }
}
