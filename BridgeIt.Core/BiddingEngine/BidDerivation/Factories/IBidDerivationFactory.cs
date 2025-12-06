using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.BidDerivation.Factories;

public interface IBidDerivationFactory
{
    bool CanCreate(string key); 
    IBidDerivation Create(Dictionary<string,object> dict);
}

public class ResponderBidDerivationFactory : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "responder";

    public IBidDerivation Create(Dictionary<string, object> dict)
    {
        return new ResponderBidDerivation();
    }
}