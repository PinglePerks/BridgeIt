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
    
    public override bool Equals(object? obj) =>
        obj is Bid other && Type == other.Type && Level == other.Level && Suit == other.Suit;

    public override int GetHashCode() => HashCode.Combine(Type, Level, Suit);

    public static bool operator ==(Bid? a, Bid? b) =>
        ReferenceEquals(a, b) || (a is not null && a.Equals(b));

    public static bool operator !=(Bid? a, Bid? b) => !(a == b);
    
    public static int NextLevelForSuit(Suit suit, Bid? currentContract)
    {
        if (currentContract == null) return 1;
        if (currentContract.Type == BidType.NoTrumps) return currentContract.Level + 1;
        return suit <= currentContract.Suit ? currentContract.Level + 1 : currentContract.Level;
    }

    public static int NextLevelForNoTrumps(Bid? currentContract)
    {
        if (currentContract == null) return 1;
        if (currentContract.Type == BidType.NoTrumps) return currentContract.Level + 1;
        return currentContract.Level; // NT always outranks any suit at same level
    }

}