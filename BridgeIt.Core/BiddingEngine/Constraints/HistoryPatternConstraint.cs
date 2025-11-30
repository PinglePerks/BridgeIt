using BridgeIt.Core.BiddingEngine.Constraints.Factories;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class HistoryPatternConstraint : IBidConstraint
{
    private readonly List<string> _pattern;

    public HistoryPatternConstraint(List<string> pattern)
    {
        _pattern = pattern;
    }

    public bool IsMet(BiddingContext ctx)
    {
        var history = ctx.AuctionHistory.Bids;

        if (_pattern[0] == "*")
        {
            return true;
        }
        
        // Handle "Pass*"
        if (_pattern.Count == 1 && _pattern[0] == "Pass*")
        {
            // True if empty (Dealer) OR all bids are Pass
            return history.Count == 0 || history.All(b => b.ChosenBid.Type == BidType.Pass);
        }
        
        // Pattern match
        if (_pattern[0] == "Pass*")
        {
            // Calculate how many extra passes there are at the start of history
            var difference = history.Count - _pattern.Count;
    
            // The first 'difference' items in history should all be Pass
            if (!history.Take(difference).All(h => h.ChosenBid.Type == BidType.Pass))
                return false;
    
            // Now check that the remaining history matches the pattern (skipping "Pass*")
            for (var i = 1; i < _pattern.Count; i++)
            {
                var historyIndex = difference + i;
                
                if (_pattern[i] != history[historyIndex].ChosenBid.ToString() && _pattern[i] != "*")
                    return false;
            }
    
            return true; // Pattern matches
        }
        
        
        

        // Exact Match Logic
        // if (history.Count != _pattern.Count) return false;
        //
        // for (int i = 0; i < history.Count; i++)
        // {
        //     if (history[i].ChosenBid.ToString() != _pattern[i]) return false;
        // }
        // return true;
        return false;
    }
}

public class HistoryPatternConstraintFactory : IConstraintFactory
{
    public bool CanCreate(string key) => key == "history_pattern";

    public IBidConstraint Create(object value)
    {
        // Convert Yaml list to List<string>
        var list = ((List<object>)value).Select(x => x.ToString()).ToList();
        return new HistoryPatternConstraint(list);
    }
}