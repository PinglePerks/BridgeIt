using BridgeIt.Core.BiddingEngine.Core;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class LosingTrickCountConstraint : IBidConstraint
{
    public readonly int Max = 13;
    public readonly int Min = 0;
    
    public LosingTrickCountConstraint(string rangeString)
    {
        if (rangeString.Contains('-'))
        {
            var parts = rangeString.Split('-');
            Min = int.Parse(parts[0]);
            Max = int.Parse(parts[1]);
        } 
        else if (rangeString.StartsWith("<="))
        {
            Max = int.Parse(rangeString.Substring(2));
            
        } 
        else if (rangeString.StartsWith(">="))
        {
            Min = int.Parse(rangeString.Substring(2));
        }
        else
        {
            Max = int.Parse(rangeString);
            Min = Max;
        }
    }
    
    public bool IsMet(BiddingContext ctx)
    {
        var losers = ctx.HandEvaluation.Losers;
        return  losers >= Min && losers <= Max;
    }
}