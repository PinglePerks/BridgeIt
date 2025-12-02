using System.Collections;

namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class SeatRoleConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "seat_role";

    public IBidConstraint Create(object value)
    {
        return new SeatRoleConstraint(value.ToString()!);
    }
}