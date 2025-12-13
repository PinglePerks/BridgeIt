using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Domain.Bidding;

public sealed class Bid
{
    public BidType Type { get; }
    public int Level { get; }
    public Suit? Suit { get; }

    private Bid(BidType type, int level, Suit? suit)
    {
        Type = type;
        Level = level;
        Suit = suit;
    }
    
    public static Bid Pass() => new(BidType.Pass, 0, null);
    public static Bid Double() => new(BidType.Double, 0, null);
    public static Bid Redouble() => new(BidType.Redouble, 0, null);

    public static Bid SuitBid(int level, Suit suit)
        => new(BidType.Suit, level, suit);
    
    public static Bid NoTrumpsBid(int level)
        => new(BidType.NoTrumps, level, null);

    public override string ToString()
    {
        return Type switch
        {
            BidType.Pass => "Pass",
            BidType.Double => "X",
            BidType.Redouble => "XX",
            BidType.Suit => $"{Level}{Suit.Value.ToShortString()}",
            BidType.NoTrumps => $"{Level}NT",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}