namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;



public interface IConstraintFactory
{
    // Returns true if this factory handles the specific key (e.g., "hcp", "shape")
    bool CanCreate(string key); 
    IBidConstraint Create(object value);
}