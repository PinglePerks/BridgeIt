using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.WebDeal.Models;

public class CustomDealRequest
{
    public int MinHcp { get; set; } = 12;
    public int MaxHcp { get; set; } = 14;
    public int MinLosers { get; set; } = 0;
    public int MaxLosers { get; set; } = 13;
    public string HcpCheck { get; set; } = "hcp";
    public string BalancedCheck { get; set; } = "unbalanced";
    
    public Dictionary<Suit, int?> Shape { get; set; } = new();
}