using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class HcpConstraint : IBidConstraint
{
    public readonly int Min = 0;
    public readonly int Max = 40;

    public HcpConstraint(string rangeString)
    {
        if (rangeString.Contains('-'))
        {
            var parts = rangeString.Split('-');
            Min = int.Parse(parts[0]);
            Max = int.Parse(parts[1]);
        }
        else if (rangeString.StartsWith(">="))
        {
            Min = int.Parse(rangeString.Substring(2));
        }
        else if (rangeString.StartsWith("<="))
        {
            Max = int.Parse(rangeString.Substring(2));
        }
        else
        {
            Min = int.Parse(rangeString);
            Max = Min;
        }
    }

    public bool IsMet(DecisionContext ctx)
    {
        int hcp = ctx.HandEvaluation.Hcp;
        return hcp >= Min && hcp <= Max;
    }
}