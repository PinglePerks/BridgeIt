using System.Collections;

namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class PartnerKnowledgeConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "partner_knowledge";

    public IBidConstraint Create(object value)
    {
        // YamlDotNet deserializes nested objects as Dictionary<object, object>
        // We need to cast it safely
        if (value is IDictionary rawDict)
        {
            var cleanDict = new Dictionary<string, string>();
            
            foreach (DictionaryEntry entry in rawDict)
            {
                cleanDict[entry.Key.ToString()] = entry.Value.ToString();
            }

            return new PartnerKnowledgeConstraint(cleanDict);
        }

        throw new ArgumentException($"Invalid knowledge constraint format. Expected Dictionary, got {value.GetType().Name}");
    }
}