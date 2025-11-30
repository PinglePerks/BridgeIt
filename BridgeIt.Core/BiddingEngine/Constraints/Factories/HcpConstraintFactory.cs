namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class HcpConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "hcp";

    public IBidConstraint Create(object value)
    {
        // Value comes from YAML as string "12-14"
        return new HcpConstraint(value.ToString());
    }
}
