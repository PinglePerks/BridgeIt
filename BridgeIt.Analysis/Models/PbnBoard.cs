using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Analysis.Models;

public class PbnBoard
{
    public string Event { get; set; }
    public string BoardNumber { get; set; }
    public Seat Dealer { get; set; }
    public Vulnerability Vulnerability { get; set; }
    
    // The Deal: North, East, South, West hands
    public Dictionary<Seat, Hand> Hands { get; set; } = new();
    
    // The Auction: List of bid strings (e.g. "Pass", "1H", "X")
    public List<string> ActualAuction { get; set; } = new();
}