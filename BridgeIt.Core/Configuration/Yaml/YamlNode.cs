using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using YamlDotNet.Serialization;

namespace BridgeIt.Core.Configuration.Yaml;

public class YamlNode
{
    public string Bid { get; set; } = string.Empty;
    public Dictionary<string, object> DynamicBid { get; set; } = new();
    public string Type { get; set; }
    public PartnershipBiddingState Meaning { get; set; }
    public PartnershipBiddingState NextState { get; set; }
    public Dictionary<string, object> Constraints { get; set; } = new();
}