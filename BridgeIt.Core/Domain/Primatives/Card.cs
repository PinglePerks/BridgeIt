namespace BridgeIt.Core.Domain.Primatives;

public readonly record struct Card(Suit Suit, Rank Rank)
{
    public override string ToString()
        => $"{RankExtensions.ToString(Rank)}{Suit.ToShortString()}";

    public string ToSymbolString()
        => $"{RankExtensions.ToString(Rank)}{Suit.ToSymbol()}";
}