namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class LosingTrickCountCounstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "losers";
    public IBidConstraint Create(object value) => new LosingTrickCountConstraint(value.ToString());
}