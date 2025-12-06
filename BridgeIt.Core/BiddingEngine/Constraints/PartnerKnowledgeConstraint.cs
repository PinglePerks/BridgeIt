using System.Text;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Constraints;

public class PartnerKnowledgeConstraint : IBidConstraint
{
    public readonly Dictionary<string, string> Requirements;
    public PartnerKnowledgeConstraint(Dictionary<string, string> requirements)
    {
        Requirements = requirements;
    }

    public bool IsMet(BiddingContext ctx)
    {
        var k = ctx.PartnershipKnowledge;
        
        foreach(var req in Requirements)
        {
            switch (req.Key)
            {
                case "combined_hcp":
                    
                    if (!CheckHcpRequirement(req.Value, k.CombinedHcpMin(ctx.HandEvaluation.Hcp))) return false;
                    break;
                case "shape":
                    
                    if (req.Value == "balanced")
                    {
                        if (ctx.PartnershipKnowledge.PartnerIsBalanced) return true;
                        
                        return false;
                    }

                    break;
                
                case "fit_in_suit":
                    
                    if (req.Value == "no_major")
                    {
                        if(!k.HasFit(Suit.Hearts,ctx.HandEvaluation.Shape[Suit.Hearts]) && !k.HasFit(Suit.Spades, ctx.HandEvaluation.Shape[Suit.Spades])) return true;
                        return false;
                    }

                    if (req.Value == "any")
                    {
                        
                        if(k.HasFit(Suit.Diamonds,ctx.HandEvaluation.Shape[Suit.Diamonds]) || k.HasFit(Suit.Clubs, ctx.HandEvaluation.Shape[Suit.Clubs])) return true;
                        
                        if(k.HasFit(Suit.Hearts,ctx.HandEvaluation.Shape[Suit.Hearts]) || k.HasFit(Suit.Spades, ctx.HandEvaluation.Shape[Suit.Spades])) return true;
                        
                        return false;
                    }
                    
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