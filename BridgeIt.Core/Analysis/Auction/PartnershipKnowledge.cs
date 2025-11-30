
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Auction;

public class PartnershipKnowledge
{
    // --- High Card Points ---
    public int PartnerHcpMin { get; set; } = 0;
    public int PartnerHcpMax { get; set; } = 40;
    
    public Suit? FitInSuit { get; set; }
    
    

    // --- Suit Lengths (Min known length) ---
    public Dictionary<Suit, int> PartnerMinShape { get; } = new()
    {
        { Suit.Spades, 0 },
        { Suit.Hearts, 0 },
        { Suit.Diamonds, 0 },
        { Suit.Clubs, 0 }
    };
    public Dictionary<Suit, int> PartnerMaxShape { get; } = new()
    {
        { Suit.Spades, 13 },
        { Suit.Hearts, 13 },
        { Suit.Diamonds, 13 },
        { Suit.Clubs, 13 }
    };
    

    // --- Specific Attributes ---
    public bool PartnerIsBalanced { get; set; } = false;

    public bool PartnerDeniedMajor(int numHearts, int numSpades)
    {
        return !HasPossibleFit(Suit.Hearts, numHearts) || !HasPossibleFit(Suit.Spades, numSpades);
    }
    
    // --- Derived Helpers ---
    public int CombinedHcpMin(int myHcp) => PartnerHcpMin + myHcp;
    
    public bool HasFit(Suit suit, int myLength) 
    {
        // Standard fit is 8+ cards combined
        return (PartnerMinShape[suit] + myLength) >= 8;
    }
    

    public Suit? BestFitSuit(Dictionary<Suit, int> myHand)
    {
        foreach (var suit in PartnerMinShape.Keys)
        {
            if (HasFit(suit, myHand[suit])) return suit;
        }

        return null;
    }
    

    public bool HasPossibleFit(Suit suit, int myLength) => (PartnerMaxShape[suit] + myLength) >= 8;
}