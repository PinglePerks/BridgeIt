using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Hands;

public static class LosingTrickCount
{
    public static int Count(Hand hand)
    {
        var losers = 0;

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            var suitCards = hand.Cards
                .Where(c => c.Suit == suit)
                .OrderByDescending(c => c.Rank)
                .ToArray();

            losers += CountSuitLosers(suitCards);
        }

        return losers;
    }

    public static int CountSuitLosers(Card[] suitCards)
    {
        var length = suitCards.Length;

        if (length == 0)
            return 0;

        var losers = Math.Min(3, length);

        // Top three ranks matter
        if (suitCards[0].Rank == Rank.Ace)
            losers--;

        if (length >= 2)
            if(suitCards[1].Rank == Rank.King || suitCards[0].Rank == Rank.King)
                losers--;

        if (length >= 3)
            if(suitCards[0].Rank == Rank.Queen || suitCards[1].Rank == Rank.Queen || suitCards[2].Rank == Rank.Queen)
                losers--;

        // Adjust if suit is short
        if (length == 1) losers = Math.Min(losers, 1);
        if (length == 2) losers = Math.Min(losers, 2);

        return losers;
    }

    /// <summary>
    /// Expected tricks = 24 - (my losers + partner losers).
    /// </summary>
    public static int ExpectedTricks(int myLtc, int partnerLtc)
        => 24 - (myLtc + partnerLtc);

    /// <summary>
    /// Bid level = expected tricks - 6, clamped to 1–7.
    /// </summary>
    public static int BidLevel(int expectedTricks)
        => Math.Clamp(expectedTricks - 6, 1, 7);
}