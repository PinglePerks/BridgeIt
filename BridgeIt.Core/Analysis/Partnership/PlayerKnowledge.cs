using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Partnership;

/// <summary>
/// What we know about a single player's hand, inferred from their bids.
/// One instance per player at the table. Contains no partnership-specific
/// or combined-strength logic — purely "what has this player's bidding
/// told us about their hand."
/// </summary>
public class PlayerKnowledge
{
    public int HcpMin { get; set; } = 0;
    public int HcpMax { get; set; } = 37;

    public int LosersMin { get; set; } = 0;
    public int LosersMax { get; set; } = 13;

    public Dictionary<Suit, int> MinShape { get; set; } = new()
    {
        { Suit.Spades, 0 },
        { Suit.Hearts, 0 },
        { Suit.Diamonds, 0 },
        { Suit.Clubs, 0 }
    };

    public Dictionary<Suit, int> MaxShape { get; set; } = new()
    {
        { Suit.Spades, 13 },
        { Suit.Hearts, 13 },
        { Suit.Diamonds, 13 },
        { Suit.Clubs, 13 }
    };

    public bool IsBalanced { get; set; } = false;

    public HashSet<Suit> DeniedSuits { get; set; } = new();

    /// <summary>
    /// Have any ranges been narrowed from their uninformative defaults?
    /// This can be true from cross-table inference (opponents bidding) even if
    /// this player hasn't bid. For knowledge-based bidding decisions about
    /// partner, use PartnerLastNonPassBid != null on AuctionEvaluation instead —
    /// that confirms partner has actively communicated.
    /// </summary>
    public bool HasNarrowedRanges =>
        HcpMin > 0 || HcpMax < 37
        || LosersMin > 0 || LosersMax < 13
        || MinShape.Values.Any(v => v > 0)
        || MaxShape.Values.Any(v => v < 13)
        || IsBalanced
        || DeniedSuits.Count > 0;

    /// <summary>
    /// Does this player definitely have at least minLength cards in the given suit?
    /// </summary>
    public bool HasMinimumInSuit(Suit suit, int minLength)
        => MinShape[suit] >= minLength;

    /// <summary>
    /// Could this player possibly have maxLength or more cards in the given suit?
    /// </summary>
    public bool CouldHaveInSuit(Suit suit, int maxLength)
        => MaxShape[suit] >= maxLength;
}
