using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dds;

/// <summary>
/// Converts a deal (Dictionary&lt;Seat, Hand&gt;) to PBN format for DDS consumption.
/// DDS expects: "N:AKQJ.T987.654.32 T987.654.32.AKQJ ..."
/// (dots between suits S.H.D.C, spaces between hands in N E S W order).
/// </summary>
public static class DealConverter
{
    public static string ToPbn(Dictionary<Seat, Hand> deal)
    {
        var seatOrder = new[] { Seat.North, Seat.East, Seat.South, Seat.West };
        var hands = seatOrder.Select(seat => HandToPbnString(deal[seat]));
        return $"N:{string.Join(" ", hands)}";
    }

    private static string HandToPbnString(Hand hand)
    {
        var suitOrder = new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
        var suits = suitOrder.Select(suit =>
            new string(
                hand.Cards
                    .Where(c => c.Suit == suit)
                    .OrderByDescending(c => c.Rank)
                    .Select(c => RankToChar(c.Rank))
                    .ToArray()
            )
        );
        return string.Join(".", suits);
    }

    private static char RankToChar(Rank rank) => rank switch
    {
        Rank.Ace => 'A',
        Rank.King => 'K',
        Rank.Queen => 'Q',
        Rank.Jack => 'J',
        Rank.Ten => 'T',
        Rank.Nine => '9',
        Rank.Eight => '8',
        Rank.Seven => '7',
        Rank.Six => '6',
        Rank.Five => '5',
        Rank.Four => '4',
        Rank.Three => '3',
        Rank.Two => '2',
        _ => '?'
    };
}
