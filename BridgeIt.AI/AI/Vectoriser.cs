using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Analysis.MachineLearning;

public static class HandVectorizer
{
    // The vector size is always 52 (standard deck)
    public const int VectorSize = 52;

    public static float[] Vectorize(Hand hand)
    {
        // Initialize an array of zeros
        float[] vector = new float[VectorSize];

        // Iterate through the cards in the hand and set the corresponding index to 1
        foreach (var card in hand.Cards)
        {
            int index = GetCardIndex(card);
            vector[index] = 1.0f;
        }

        return vector;
    }

    /// <summary>
    /// Maps a Card to a unique index from 0 to 51.
    /// Order: Clubs 2-A, Diamonds 2-A, Hearts 2-A, Spades 2-A
    /// </summary>
    public static int GetCardIndex(Card card)
    {
        // Suits: Clubs=0, Diamonds=1, Hearts=2, Spades=3
        int suitOffset = (int)card.Suit * 13;

        // Ranks: 2=0, 3=1, ..., A=12
        // Assuming your Rank enum is: Two=2, Three=3... Ace=14
        int rankIndex = (int)card.Rank - 2; 

        return suitOffset + rankIndex;
    }
    
    /// <summary>
    /// Reverse operation: Useful for debugging or visualizing vectors
    /// </summary>
    public static string GetCardNameFromIndex(int index)
    {
        int suitVal = index / 13;
        int rankVal = (index % 13) + 2;
        
        var suit = (Suit)suitVal;
        var rank = (Rank)rankVal;
        
        return $"{rank.ShortName()}{suit.ToSymbol()}";
    }
}