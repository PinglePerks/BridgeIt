namespace BridgeIt.Core.BiddingEngine.BidDerivation.Factories;

public class TransferDerivationFactory : IBidDerivationFactory
{
    public bool CanCreate(string key) => key == "transfer";
    
    public IBidDerivation Create(Dictionary<string,object> dict)
    {
        return new TransferBidDerivation();
    }
}