using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public class HandFormatter : IHandFormatter
{
    public string FormatHand(Hand hand)
    {
        var clubs    = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Clubs).Select(c => c.Rank.ShortName()));
        var diamonds = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Diamonds).Select(c => c.Rank.ShortName()));
        var hearts   = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Hearts).Select(c => c.Rank.ShortName()));
        var spades   = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Spades).Select(c => c.Rank.ShortName()));

        return string.Join(clubs, diamonds, hearts, spades);
    }
}