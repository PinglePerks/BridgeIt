using System.Text.RegularExpressions;
using BridgeIt.Analysis.Models;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Analysis.Parsers;

public class PbnParser
{
    public IEnumerable<PbnBoard> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return ParseString(content);
    }

    public IEnumerable<PbnBoard> ParseString(string content)
    {
        var boards = new List<PbnBoard>();
        
        // PBN files often contain multiple games. 
        // We split by [Event ...] usually, but simply processing line-by-line is more robust 
        // given the tags can appear in various orders.
        
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        PbnBoard currentBoard = null;
        bool inAuction = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // 1. Detect New Board (Start of Event)
            if (trimmed.StartsWith("[Event "))
            {
                if (currentBoard != null) boards.Add(currentBoard);
                currentBoard = new PbnBoard();
                inAuction = false;
            }

            if (currentBoard == null) continue; // Skip header garbage if any

            // 2. Handle Auction Section (Multi-line data)
            if (inAuction)
            {
                // If we hit a new tag (starts with '['), auction is over
                if (trimmed.StartsWith("["))
                {
                    inAuction = false; 
                    // Fallthrough to process this new tag
                }
                else
                {
                    // Parse Auction Lines
                    ParseAuctionLine(trimmed, currentBoard);
                    continue; // Skip tag processing for this line
                }
            }

            // 3. Process Tags [Key "Value"]
            if (trimmed.StartsWith("["))
            {
                var match = Regex.Match(trimmed, @"\[(\w+)\s+""(.*)""\]");
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;

                    switch (key)
                    {
                        case "Event": currentBoard.Event = value; break;
                        case "Board": currentBoard.BoardNumber = value; break;
                        case "Dealer": currentBoard.Dealer = ParseSeat(value); break;
                        case "Vulnerable": currentBoard.Vulnerability = ParseVulnerability(value); break;
                        case "Deal": currentBoard.Hands = ParseDeal(value); break;
                        case "Auction": 
                            // Auction tag value is the Dealer usually, e.g. [Auction "N"]
                            // We ignore the value and start reading subsequent lines
                            inAuction = true; 
                            break;
                    }
                }
            }
        }

        // Add the final board
        if (currentBoard != null) boards.Add(currentBoard);

        return boards;
    }

    // --- Parsing Helpers ---

    private Dictionary<Seat, Hand> ParseDeal(string pbnDeal)
    {
        // Format: "N:K872.KT5.J83.KT5 A954.94.AK954.Q8 ..."
        // First char is dealer for the deal string (usually N), followed by colon.
        // Then 4 hands separated by space.
        
        var parts = pbnDeal.Split(':');
        var firstHandSeat = ParseSeat(parts[0]);
        var handsStr = parts[1].Trim().Split(' ');

        var result = new Dictionary<Seat, Hand>();
        var currentSeat = firstHandSeat;

        foreach (var handStr in handsStr)
        {
            result[currentSeat] = ParseSinglePbnHand(handStr);
            currentSeat = NextSeat(currentSeat);
        }

        return result;
    }

    private Hand ParseSinglePbnHand(string handStr)
    {
        // Format: "K872.KT5.J83.KT5" (Spades.Hearts.Diamonds.Clubs)
        // PBN standard is always S H D C order.
        var suits = handStr.Split('.');
        var cards = new List<Card>();

        if (suits.Length != 4) return new Hand(new List<Card>()); // Error or empty

        AddSuitCards(cards, Suit.Spades, suits[0]);
        AddSuitCards(cards, Suit.Hearts, suits[1]);
        AddSuitCards(cards, Suit.Diamonds, suits[2]);
        AddSuitCards(cards, Suit.Clubs, suits[3]);

        return new Hand(cards);
    }

    private void AddSuitCards(List<Card> cards, Suit suit, string ranks)
    {
        foreach (char rankChar in ranks)
        {
            // Convert PBN rank char to your Card.Parse format
            // PBN: T, J, Q, K, A. Your Card.Parse likely supports these.
            string cardStr = $"{rankChar}{suit.ShortName()}"; // e.g. "KS"
            try 
            {
                cards.Add(cardStr.ParseCard());
            }
            catch 
            {
                // Handle odd chars like '-' or placeholders
            }
        }
    }

    private void ParseAuctionLine(string line, PbnBoard board)
    {
        // Line: "Pass 1D X 1H"
        // Also might contain notes like "3D=1=" or end with "*"
        var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token == "*" || token == "+") continue; // End of auction markers
            if (string.IsNullOrWhiteSpace(token)) continue;

            // Strip notes (e.g. "3D=1=" -> "3D")
            // Assuming strict PBN, note references like =1= usually follow the bid. 
            // Simple approach: Take alphanumeric chars? 
            // Or just split by '='.
            var cleanBid = token.Split('=')[0]; 
            
            board.ActualAuction.Add(cleanBid);
        }
    }

    private Seat ParseSeat(string s) => s.ToUpper() switch
    {
        "N" => Seat.North,
        "E" => Seat.East,
        "S" => Seat.South,
        "W" => Seat.West,
        _ => Seat.North // Default fallback
    };
    
    private Seat NextSeat(Seat s) => (Seat)(((int)s + 1) % 4);

    private Vulnerability ParseVulnerability(string v) => v.ToUpper() switch
    {
        "None" => Vulnerability.None,
        "Love" => Vulnerability.None,
        "Both" => Vulnerability.Both,
        "All" => Vulnerability.Both,
        "NS" => Vulnerability.NS,
        "EW" => Vulnerability.EW,
        _ => Vulnerability.None
    };
}