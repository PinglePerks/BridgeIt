using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Api.Models;

public class CustomDealRequest
{
    public int MinHcp { get; set; }
    public int MaxHcp { get; set; }
    public int MinLosers { get; set; } 
    public int MaxLosers { get; set; } 
    public string HcpCheck { get; set; } = string.Empty;
    public string BalancedCheck { get; set; } = string.Empty;
    
    public Dictionary<Suit, int?> Shape { get; set; } = new();
}