namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class CurrentContractConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "current_contract";

    public IBidConstraint Create(object value)
    {
        // Value comes from YAML as string "12-14"
        return new HcpConstraint(value.ToString());
    }
}
