namespace BridgeIt.Core.Domain.Primatives;

public readonly record struct Card(Suit Suit, Rank Rank)
{
    public override string ToString()
        => $"{Rank.ShortName()}{Suit.ShortName()}";

    public string ToSymbolString()
        => $"{Rank.ShortName()}{Suit.ToSymbol()}";
}