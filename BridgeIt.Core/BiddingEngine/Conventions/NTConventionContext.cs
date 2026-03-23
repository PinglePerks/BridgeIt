using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Conventions;

/// <summary>
/// Describes an auction context where partner has shown a balanced NT hand,
/// enabling Stayman and transfer conventions at the appropriate level.
///
/// Shared by both responder-side rules (StandardStayman, StandardTransfer)
/// and opener-side rules (StaymanResponse, CompleteTransfer).
/// </summary>
public class NTConventionContext
{
    /// <summary>Human-readable label for this context (e.g. "1NT", "2NT", "2C-2D-2NT")</summary>
    public required string Name { get; init; }

    /// <summary>The level at which conventions operate (2 after 1NT, 3 after 2NT)</summary>
    public required int ConventionLevel { get; init; }

    /// <summary>Minimum HCP for responder to use Stayman in this context</summary>
    public required int StaymanHcpMin { get; init; }

    /// <summary>Predicate: is this the right auction state for a responder to use conventions?</summary>
    public required Func<AuctionEvaluation, bool> ResponderIsTriggered { get; init; }

    // ── Derived bids ────────────────────────────────────────────────

    /// <summary>The NT bid level that established this context (ConventionLevel - 1)</summary>
    public int NTLevel => ConventionLevel - 1;

    /// <summary>The Stayman bid (clubs at ConventionLevel)</summary>
    public Bid StaymanBid => Bid.SuitBid(ConventionLevel, Suit.Clubs);

    /// <summary>The transfer bid showing hearts (diamonds at ConventionLevel)</summary>
    public Bid HeartTransferBid => Bid.SuitBid(ConventionLevel, Suit.Diamonds);

    /// <summary>The transfer bid showing spades (hearts at ConventionLevel)</summary>
    public Bid SpadeTransferBid => Bid.SuitBid(ConventionLevel, Suit.Hearts);

    /// <summary>The transfer completion for hearts</summary>
    public Bid HeartCompletionBid => Bid.SuitBid(ConventionLevel, Suit.Hearts);

    /// <summary>The transfer completion for spades</summary>
    public Bid SpadeCompletionBid => Bid.SuitBid(ConventionLevel, Suit.Spades);
}

public static class NTConventionContexts
{
    public static readonly NTConventionContext After1NT = new()
    {
        Name = "1NT",
        ConventionLevel = 2,
        StaymanHcpMin = 11,
        ResponderIsTriggered = a =>
            a.BiddingRound == 1
            && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(1)
            && a.OpeningBid == Bid.NoTrumpsBid(1)
    };

    public static readonly NTConventionContext After2NT = new()
    {
        Name = "2NT",
        ConventionLevel = 3,
        StaymanHcpMin = 4,
        ResponderIsTriggered = a =>
            a.BiddingRound == 1
            && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(2)
            && a.OpeningBid == Bid.NoTrumpsBid(2)
    };

    public static readonly NTConventionContext After2C2D2NT = new()
    {
        Name = "2C-2D-2NT",
        ConventionLevel = 3,
        StaymanHcpMin = 0, // Partner has 23+, even very weak hands should explore
        ResponderIsTriggered = a =>
            a.BiddingRound == 2
            && a.PartnerLastNonPassBid == Bid.NoTrumpsBid(2)
            && a.OpeningBid == Bid.SuitBid(2, Suit.Clubs)
    };
}
