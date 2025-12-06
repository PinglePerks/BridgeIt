namespace BridgeIt.Core.BiddingEngine.BidDerivation.Factories;

public class LengthBidDerivationFactory : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "length_bid";
    
    public IBidDerivation Create(Dictionary<string,object> dict)
    {
        dict.TryGetValue("forbidden", out var value);
        return new LengthBidDerivation(dict["type"].ToString(), value as string);
    }
}