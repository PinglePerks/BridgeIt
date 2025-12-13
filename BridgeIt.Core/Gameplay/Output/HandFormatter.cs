using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public class HandFormatter : IHandFormatter
{
    public string FormatHand(Hand hand)
    {
        var clubs    = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Clubs).Select(c => RankExtensions.ToString(c.Rank)));
        var diamonds = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Diamonds).Select(c => RankExtensions.ToString(c.Rank)));
        var hearts   = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Hearts).Select(c => RankExtensions.ToString(c.Rank)));
        var spades   = string.Join(" ", hand.Cards.Where(c => c.Suit == Suit.Spades).Select(c => RankExtensions.ToString(c.Rank)));

        return string.Join(clubs, diamonds, hearts, spades);
    }
}