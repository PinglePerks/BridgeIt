using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation.Factories;

public interface IBidDerivationFactory
{
    bool CanCreate(string key); 
    IBidDerivation Create(Dictionary<string,object> dict);
}

public class LengthBidDerivationFactory : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "length_bid";
    
    public IBidDerivation Create(Dictionary<string,object> dict)
    {
        return new LengthBidDerivation();
    }
}

public class SimpleRaiseDerivationFactor : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "simple_raise";
    
    public IBidDerivation Create(Dictionary<string,object> dict)
    {
        if(!dict.ContainsKey("level")) throw new ArgumentException("Simple raise derivation must have a level");
        if(!int.TryParse((string)dict["level"], out _)) throw new ArgumentException("Simple raise derivation level must be an integer");
        return new SimpleRaise(int.Parse((string)dict["level"]));
    }
}