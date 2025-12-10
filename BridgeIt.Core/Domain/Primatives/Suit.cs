using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.Domain.Primatives;

public enum Suit
{
    Clubs = 0,
    Diamonds = 1,
    Hearts = 2,
    Spades = 3
}

public static class SuitExtensions
{
    public static string ToShortString(this Suit suit)
        => suit switch
        {
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            Suit.Spades => "S",
            _ => "?"
        };

    public static char ToSymbol(this Suit suit) =>
        suit switch
        {
            Suit.Clubs => '♣',
            Suit.Diamonds => '♦',
            Suit.Hearts => '♥',
            Suit.Spades => '♠',
            _ => '?'
        };
    




 

 

}