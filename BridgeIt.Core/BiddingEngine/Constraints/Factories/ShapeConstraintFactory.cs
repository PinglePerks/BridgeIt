using System.Collections;

namespace BridgeIt.Core.BiddingEngine.Constraints.Factories;

public class ShapeConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "shape";

    public IBidConstraint Create(object value)
    {
        // Case A: shape: "balanced"
        if (value is string strVal && strVal.ToLower() == "balanced")
        {
            return new BalancedConstraint();
        }

        // Case B: shape: { hearts: ">= 4", spades: ">= 4" }
        // YamlDotNet deserializes nested objects as Dictionary<object, object> or Dictionary<string, object>
        if (value is IDictionary dict)
        {
            var composite = new CompositeConstraint();
            foreach (DictionaryEntry entry in dict)
            {
                string suitName = entry.Key.ToString();
                string lengthExpr = entry.Value.ToString();
                composite.Add(new SuitLengthConstraint(suitName, lengthExpr));
            }
            return composite;
        }

        throw new ArgumentException($"Invalid shape constraint format: {value}");
    }
}