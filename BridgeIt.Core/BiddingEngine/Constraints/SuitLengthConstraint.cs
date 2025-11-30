using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class SuitLengthConstraint : IBidConstraint
{
    public Suit Suit; // "hearts", "spades", etc
    public readonly int MinLen = 0;
    public readonly int MaxLen = 14;
    private readonly Suit? _suit = null;

    public SuitLengthConstraint(string suit, string lengthExpression)
    {
        if (suit == "any")
            _suit = null;
        else
            _suit = suit.ToSuit();
        if (lengthExpression.StartsWith(">="))
        {
            MinLen = int.Parse(lengthExpression.Replace(">=", "").Trim());
        }
        else if (lengthExpression.StartsWith("<="))
        {
            MaxLen = int.Parse(lengthExpression.Replace("<=", "").Trim());
        }
        
    }

    public bool IsMet(BiddingContext ctx)
    {
        // 3. Handle the NULL case (Any suit matches criteria)
        if (_suit == null)
        {
            // We just check if *any* suit returned is not null
            var suit = GetLongestMatchingSuit(ctx);
            if (suit != null)
            {
                Suit = suit.Value;
                return true;
            }
            return false;
        }

        // 4. Use Suit.Value to satisfy the Dictionary's non-nullable key requirement
        ctx.HandEvaluation.Shape.TryGetValue(_suit.Value, out int count);
        
        Suit = _suit.Value;
        
        return count >= MinLen && count <= MaxLen;
    }

    // Logic: Returns the first Suit that meets the criteria, or null if none
    // Changed from returning bool to returning Suit?
    protected internal Suit? GetLongestMatchingSuit(BiddingContext ctx)
    {
        // We iterate through the shape dictionary to find a match
        foreach(var kvp in ctx.HandEvaluation.Shape)
        {
            int count = kvp.Value;
            if (count >= MinLen && count <= MaxLen) 
            {
                return kvp.Key;
            }
        }
        return null;
    }
}