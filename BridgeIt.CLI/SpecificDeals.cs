using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.CLI;

public class SpecificDeals
{
    public Dictionary<Seat, Hand> DealRandom()
    {
        var deck = new Deck();
        deck.Shuffle();
        return new Dictionary<Seat, Hand>
        {
            [Seat.North] = new Hand(deck.Cards.Take(13)),
            [Seat.East] = new Hand(deck.Cards.Skip(13).Take(13)),
            [Seat.South] = new Hand(deck.Cards.Skip(26).Take(13)),
            [Seat.West] = new Hand(deck.Cards.Skip(39).Take(13))
        };
    }

    public Dictionary<Seat, Hand> Deal1NT()
    {
        // 1. Define North: Balanced (4-3-3-3), 13 HCP
// S: K J x x (4) | H: Q x x (2) | D: K x x (3) | C: A x x (4) = 13 Total
        var northCards = new List<string> 
        { 
            "KS", "JS", "4S", "3S", 
            "QH", "5H", "2H", 
            "KD", "5D", "2D", 
            "AC", "5C", "2C" 
        };

// 3. Create the Hands using your extension
// (Assuming ParseHand returns a Hand object)
        var handNorth = northCards.ParseHand();

// 4. Handle the Deck for East/West
// We need to verify which cards are already taken so we don't deal duplicates
        var allUsedCards = new HashSet<string>(northCards);
        var fullDeck = new Deck(); // Creates a fresh 52-card deck
        fullDeck.Shuffle();

// Filter out the used cards to get the remaining 26
        var remainingCards = fullDeck.Cards
            .Where(c => !allUsedCards.Contains(c.ToSymbolString()) && !allUsedCards.Contains(c.ToString())) 
            .ToList();
        
// 5. Initialize the Dictionary
        return new Dictionary<Seat, Hand>
        {
            [Seat.North] = handNorth,
            [Seat.South] = new Hand(remainingCards.Take(13)),
            // Deal the remaining 26 cards to East and West
            [Seat.East]  = new Hand(remainingCards.Skip(13).Take(13)),
            [Seat.West]  = new Hand(remainingCards.Skip(26).Take(13))
        };
    }
    
    
    public Dictionary<Seat, Hand> GameGoingHand()
    {
        // 1. Define North: Balanced (4-3-3-3), 13 HCP
// S: K J x x (4) | H: Q x x (2) | D: K x x (3) | C: A x x (4) = 13 Total
        var northCards = new List<string> 
        { 
            "KS", "JS", "4S", "3S", 
            "AH", "5H", "2H", 
            "KD", "5D", "2D", 
            "AC", "5C", "2C" 
        };

// 2. Define South: Partner with 5+ Hearts
// H: K J T 9 8 (5) | S: A Q x | D: A Q x | C: K Q (Rest filled with high cards for fun)
        var southCards = new List<string> 
        { 
            "KH", "JH", "TH", "9H", "8H", 
            "AS", "QS", "TS", 
            "AD", "QD", "TD", 
            "KC", "QC" 
        };

// 3. Create the Hands using your extension
// (Assuming ParseHand returns a Hand object)
        var handNorth = northCards.ParseHand();
        var handSouth = southCards.ParseHand();

// 4. Handle the Deck for East/West
// We need to verify which cards are already taken so we don't deal duplicates
        var allUsedCards = new HashSet<string>(northCards.Concat(southCards));
        var fullDeck = new Deck(); // Creates a fresh 52-card deck

// Filter out the used cards to get the remaining 26
        var remainingCards = fullDeck.Cards
            .Where(c => !allUsedCards.Contains(c.ToSymbolString()) && !allUsedCards.Contains(c.ToString())) 
            // Note: Depending on your Parse implementation, you might need to match by value, not string representation.
            // A safer way if you have Card objects is:
            .Where(c => !handNorth.Cards.Contains(c) && !handSouth.Cards.Contains(c))
            .ToList();

// 5. Initialize the Dictionary
        return new Dictionary<Seat, Hand>
        {
            [Seat.North] = handNorth,
            [Seat.South] = handSouth,
            // Deal the remaining 26 cards to East and West
            [Seat.East]  = new Hand(remainingCards.Take(13)),
            [Seat.West]  = new Hand(remainingCards.Skip(13).Take(13))
        };
    }
    
        public Dictionary<Seat, Hand> Deal1NTTransfer()
    {
        // 1. Define North: Balanced (4-3-3-3), 13 HCP
// S: K J x x (4) | H: Q x x (2) | D: K x x (3) | C: A x x (4) = 13 Total
        var northCards = new List<string> 
        { 
            "KS", "JS", "4S", "3S", 
            "QH", "5H", "2H", 
            "KD", "5D", "2D", 
            "AC", "5C", "2C" 
        };

// 2. Define South: Partner with 5+ Hearts
// H: K J T 9 8 (5) | S: A Q x | D: A Q x | C: K Q (Rest filled with high cards for fun)
        var southCards = new List<string> 
        { 
            "KH", "JH", "TH", "9H", "8H", 
            "AS", "QS", "TS", 
            "AD", "QD", "TD", 
            "KC", "QC" 
        };

// 3. Create the Hands using your extension
// (Assuming ParseHand returns a Hand object)
        var handNorth = northCards.ParseHand();
        var handSouth = southCards.ParseHand();

// 4. Handle the Deck for East/West
// We need to verify which cards are already taken so we don't deal duplicates
        var allUsedCards = new HashSet<string>(northCards.Concat(southCards));
        var fullDeck = new Deck(); // Creates a fresh 52-card deck

// Filter out the used cards to get the remaining 26
        var remainingCards = fullDeck.Cards
            .Where(c => !allUsedCards.Contains(c.ToSymbolString()) && !allUsedCards.Contains(c.ToString())) 
            // Note: Depending on your Parse implementation, you might need to match by value, not string representation.
            // A safer way if you have Card objects is:
            .Where(c => !handNorth.Cards.Contains(c) && !handSouth.Cards.Contains(c))
            .ToList();

// 5. Initialize the Dictionary
        return new Dictionary<Seat, Hand>
        {
            [Seat.North] = handNorth,
            [Seat.South] = handSouth,
            // Deal the remaining 26 cards to East and West
            [Seat.East]  = new Hand(remainingCards.Take(13)),
            [Seat.West]  = new Hand(remainingCards.Skip(13).Take(13))
        };
    }
}