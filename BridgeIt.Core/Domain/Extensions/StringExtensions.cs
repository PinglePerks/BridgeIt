using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Domain.Extensions;

public static class StringExtensions
{
    public static Suit ToSuit(this string suitString)
    {
        if (SuitMap.TryGetValue(suitString, out Suit suit))
        {
            return suit;
        }

        throw new ArgumentException(
            $"'{suitString}' is not a valid bridge suit name (expected: Spades, Hearts, Diamonds, Clubs).",
            nameof(suitString));
    }
    
    private static readonly Dictionary<string, Suit> SuitMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "spades", Suit.Spades },
        { "hearts", Suit.Hearts },
        { "diamonds", Suit.Diamonds },
        { "clubs", Suit.Clubs },
        { "s", Suit.Spades },
        { "h", Suit.Hearts },
        { "d", Suit.Diamonds },
        { "c", Suit.Clubs },
    };
    
    public static Bid ToBid(this string bidString)
    {
        bidString = bidString.Trim().ToUpperInvariant();

        // Handle X / XX before anything else
        if (bidString == "X")
            return Bid.Double();

        if (bidString == "XX")
            return Bid.Redouble();
        
        if (bidString == "PASS")
            return Bid.Pass();
        
        if (bidString.Length < 2 || !char.IsDigit(bidString[0]))
            throw new ArgumentException($"'{bidString}' is not a valid bid (must start with level 1–7).",
                nameof(bidString));

        int level = bidString[0] - '0';

        if (level < 1 || level > 7)
            throw new ArgumentException($"Bid level must be between 1 and 7 (got {level}).", nameof(bidString));

        string contract = bidString.Substring(1); // remaining chars

        // NT bid
        if (contract == "NT")
            return Bid.NoTrumpsBid(level);

        // Single-letter suit bids only (C, D, H, S)
        if (contract.Length == 1)
        {
            return Bid.SuitBid(level, contract.ToSuit());
        }
        
        if (contract == "Pass")
            return Bid.Pass();

        // If not NT and not single suit letter → invalid
        throw new ArgumentException(
            $"'{bidString}' is not a valid bridge bid (expected:Pass, 1C–7S or 1NT–7NT, plus X/XX).",
            nameof(bidString));
    }
    
    public static Hand ToHand(this string handStr)
    {
        var parts = handStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cards = new List<Card>();

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            // First char is suit (e.g., 'S')
            string suitChar = part[0].ToString();
            Suit suit = suitChar.ToSuit(); // Uses SuitExtensions

            // Remaining chars are ranks (e.g., 'A', 'K')
            string ranks = part.Substring(1);
            foreach (char rankChar in ranks)
            {
                // Convert single char rank back to string for parsing
                // Handle 'T' for Ten if needed, though Card.Parse usually expects full string like "10" or "T"
                string rankStr = rankChar.ToString();
                
                // We need to construct a full card string for Card.Parse, e.g. "AS", "KS"
                // But Card.Parse expects "RankSuit" or "SuitRank"? 
                // Actually Card.Parse in your code expects "8C" (Rank then Suit).
                // So we construct "{Rank}{Suit}"
                
                string cardString = $"{rankStr}{suitChar}";
                
                cards.Add(cardString.ToCard());
            }
        }

        return new Hand(cards); 
    }
    
    public static Card ToCard(this string cardString)
    {
        if (string.IsNullOrWhiteSpace(cardString) || cardString.Length < 2)
            throw new ArgumentException($"Invalid card string: '{cardString}'", nameof(cardString));

        cardString = cardString.Trim().ToUpperInvariant();
        
        string suitStr = cardString[^1].ToString();
        Suit suit = suitStr.ToSuit(); 
        
        string rankStr = cardString[..^1];
        Rank rank = ToRank(rankStr);
        
        return new Card(suit, rank);
        
    }
    
    public static SeatRoleType ToSeatRole(this string seatStr)
        => seatStr.ToLower() switch
        {
            "opener" => SeatRoleType.Opener,
            "responder" => SeatRoleType.Responder,
            "overcaller" => SeatRoleType.Overcaller,
            _ => throw new ArgumentException($"Invalid seat role: '{seatStr}'")
        };
    
    public static Hand ToHand(this List<string> cardStrings)
    {
        return new Hand(cardStrings.Select(ToCard).ToList());
    }

 
    
    private static Rank ToRank(string rankStr)
    {
        return rankStr switch
        {
            "2" => Rank.Two,
            "3" => Rank.Three,
            "4" => Rank.Four,
            "5" => Rank.Five,
            "6" => Rank.Six,
            "7" => Rank.Seven,
            "8" => Rank.Eight,
            "9" => Rank.Nine,
            "T" or "10" => Rank.Ten,
            "J" => Rank.Jack,
            "Q" => Rank.Queen,
            "K" => Rank.King,
            "A" => Rank.Ace,
            _ => throw new ArgumentException($"Invalid rank: '{rankStr}'")
        };
    }
    
}