using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class PartnerKnowledgeConstraint : IBidConstraint
{
    private readonly Dictionary<string, string> _requirements;
    public PartnerKnowledgeConstraint(Dictionary<string, string> requirements)
    {
        _requirements = requirements;
    }

    public bool IsMet(BiddingContext ctx)
    {
        var k = ctx.PartnershipKnowledge;
        
        foreach(var req in _requirements)
        {
            switch (req.Key)
            {
                case "combined_hcp":
                    if (!CheckHcpRequirement(req.Value, k.CombinedHcpMin(ctx.HandEvaluation.Hcp))) return false;
                    break;
                
                case "fit_in_suit":
                    var suit = req.Value.ToSuit();
                    if(!k.HasFit(suit, ctx.HandEvaluation.Shape[suit])) return false;
                    break;
                
                case "denied_major_fit":
                    if(!ctx.PartnershipKnowledge.PartnerDeniedMajor(ctx.HandEvaluation.Shape[Suit.Hearts],
                           ctx.HandEvaluation.Shape[Suit.Spades])) return false;
                    break;
                
                default:
                    Console.WriteLine($"Unknown partner knowledge constraint: {req.Key}");
                    break;
            }
        }
        return true;
    }

    protected internal virtual bool CheckHcpRequirement(string rangeString, int minHcp)
    {
        var min = 0;
        var max = 40;
        
        if (rangeString.Contains('-'))
        {
            var parts = rangeString.Split('-');
            min = int.Parse(parts[0]);
            max = int.Parse(parts[1]);
        }
        else if (rangeString.StartsWith(">="))
        {
            min = int.Parse(rangeString.Substring(2));
            max = 40;
        }
        else
        {
            min = int.Parse(rangeString);
            max = min;
        }
        
        return min <= minHcp && minHcp <= max;
    }
}