using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.TestHarness.DebugTests;

public static class SimpleHandParser
{
    // Expected Format:
    // "North: Q9543 4 K9 98653\nEast: KJ6 AT63 QT6 J74\n..."
    // Suits are assumed to be in order: Spades, Hearts, Diamonds, Clubs
    
    public static Dictionary<Seat, Hand> ParseBoard(string input)
    {
        var hands = new Dictionary<Seat, Hand>();
        
        // Split by lines to process each player
        var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Example line: "North: Q9543 4 K9 98653"
            
            // 1. Split Seat from Cards
            var replace = line.Replace("[", "");
            var cleanStr = replace.Replace("]", "");
            var parts = cleanStr.Split(':');
            if (parts.Length != 2) continue;

            var seatStr = parts[0].Trim();
            var cardsStr = parts[1].Trim();

            // 2. Parse Seat
            if (!Enum.TryParse<Seat>(seatStr, true, out var seat))
            {
                throw new ArgumentException($"Unknown seat: {seatStr}");
            }

            // 3. Parse Cards
            // The cards string is space-separated by suit: "S H D C"
            // e.g. "Q9543 4 K9 98653" -> ["Q9543", "4", "K9", "98653"]
            var suitHoldings = cardsStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (suitHoldings.Length != 4)
            {
                throw new ArgumentException($"Invalid hand format for {seat}. Expected 4 suit groups, got {suitHoldings.Length}.");
            }

            var hand = ParseSingleHand(suitHoldings);
            hands[seat] = hand;
        }

        // Validation: Ensure we have all 4 seats
        if (hands.Count != 4)
        {
            // Optional: handle partial boards if needed, otherwise throw
            // throw new ArgumentException("Input did not contain all 4 hands.");
        }

        return hands;
    }

    private static Hand ParseSingleHand(string[] suitHoldings)
    {
        // suitHoldings[0] = Spades
        // suitHoldings[1] = Hearts
        // suitHoldings[2] = Diamonds
        // suitHoldings[3] = Clubs
        
        var cards = new List<Card>();

        AddCards(cards, Suit.Spades, suitHoldings[0]);
        AddCards(cards, Suit.Hearts, suitHoldings[1]);
        AddCards(cards, Suit.Diamonds, suitHoldings[2]);
        AddCards(cards, Suit.Clubs, suitHoldings[3]);

        return new Hand(cards);
    }

    private static void AddCards(List<Card> cards, Suit suit, string ranks)
    {
        // 'ranks' is a string like "Q9543" or "-" or ""
        if (ranks == "-") return; 

        foreach (char r in ranks)
        {
            // Map char to Rank. Your existing Card.Parse logic might be reusable,
            // but direct mapping is faster/safer for single chars.
            // Or construct the string "QS" and use your existing Card.Parse extension.
            
            string cardString = $"{r}{suit.ToShortString()}"; // e.g. "Q" + "S" -> "QS"
            
            try 
            {
                // Assuming you have a static Parse or Extension on string
                // Adjust this call to match your exact Card.Parse implementation
                cards.Add(cardString.ToCard()); 
            }
            catch
            {
                Console.WriteLine($"Warning: Could not parse card '{cardString}'");
            }
        }
    }
}