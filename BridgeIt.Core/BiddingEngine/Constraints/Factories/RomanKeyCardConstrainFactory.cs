namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class RomanKeyCardConstrainFactory : IConstraintFactory
{
    public bool CanCreate(string key)
    {
        return key == "roman_key_card";
    }

    public IBidConstraint Create(object value)
    {
        return new RomanKeyCardConstraint(int.Parse(value.ToString()));
    }
}