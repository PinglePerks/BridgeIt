namespace BridgeIt.Core.BiddingEngine.BidDerivation.Factories;

public class OneLevelResponderBidDerivationFactory : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "one_level_responder";

    public IBidDerivation Create(Dictionary<string, object> dict)
    {
        return new OneLevelResponderBidDerivation();
    }
}