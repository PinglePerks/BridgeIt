using System.Collections;

namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class CurrentContractConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "current_contract";

    public IBidConstraint Create(object value)
    {
        if (value is IDictionary rawDict)
        {
            var cleanDict = new Dictionary<string, string>();
            
            foreach (DictionaryEntry entry in rawDict)
            {
                cleanDict[entry.Key.ToString()] = entry.Value.ToString();
            }
            
            cleanDict.TryGetValue("level", out var level);
            cleanDict.TryGetValue("suit", out var suit);

            return new CurrentContractConstraint(level);
        }

        throw new ArgumentException($"Invalid current contract constraint format. Expected Dictionary, got {value.GetType().Name}");

        return new CurrentContractConstraint(value.ToString());
    }
}
