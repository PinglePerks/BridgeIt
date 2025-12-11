using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Partnership;

public static class PartnershipEvaluator {
    
    public static PartnershipKnowledge AnalyzeKnowledge(List<BidInformation> bidInfo)
    {
        var knowledge = new PartnershipKnowledge();
        
        knowledge.CurrentPartnershipState = bidInfo.LastOrDefault()?.PartnershipState;

        foreach (var bidConstraint in bidInfo.Select(b => b.Constraint))
        {
            knowledge = ExtractKnowledgeFromConstraint(bidConstraint, knowledge);
        }
        
        return knowledge;
    }
    
    // public static PartnershipKnowledge AnalyzeKnowledgeOfMe(AuctionHistory history, Seat mySeat, Hand myHand, PartnershipKnowledge knowledge, Dictionary<Seat, List<IBidConstraint>> bidConstraints)
    // {
    //     var partnerBids = history.GetAllPartnerBids(mySeat);
    //
    //     var partnerSeatIndex = ((int)mySeat + 2) % 4;
    //
    //     var partnerSeat = (Seat)partnerSeatIndex;
    //     
    //     var partnerConstraints = bidConstraints[partnerSeat];
    //     
    //     foreach (var c in partnerConstraints)
    //         knowledge = ExtractKnowledgeFromConstraint(c, knowledge);
    //
    //     var handShape = ShapeEvaluator.GetShape(myHand);
    //     
    //     knowledge.FitInSuit = knowledge.BestFitSuit(handShape);
    //     
    //     
    //     return knowledge;
    // }
    //
    internal static PartnershipKnowledge ExtractKnowledgeFromConstraint(IBidConstraint constraint, PartnershipKnowledge knowledge)
    {
        switch (constraint)
        {
            case CompositeConstraint composite:

                foreach (var child in composite.Constraints)
                {
                    knowledge = ExtractKnowledgeFromConstraint(child, knowledge);
                }
                return knowledge;
            
            case HcpConstraint hcpConstraint:
                knowledge.PartnerHcpMax = Math.Min(knowledge.PartnerHcpMax, hcpConstraint.Max);
                
                knowledge.PartnerHcpMin = Math.Max(knowledge.PartnerHcpMin, hcpConstraint.Min);
                return knowledge;
            
            case BalancedConstraint balancedConstraint:
                knowledge.PartnerIsBalanced = true;
                foreach (Suit s in Enum.GetValues(typeof(Suit)))
                {
                    knowledge.PartnerMinShape[s] = 2;
                    knowledge.PartnerMaxShape[s] = 5;
                }
                
                return knowledge;
            
            case SuitLengthConstraint suitLengthConstraint:
                if (suitLengthConstraint.Suit == null) return knowledge;

                knowledge.PartnerMinShape[suitLengthConstraint.Suit!.Value] = Math.Max(suitLengthConstraint.MinLen,
                    knowledge.PartnerMinShape[suitLengthConstraint.Suit!.Value]);

                knowledge.PartnerMaxShape[suitLengthConstraint.Suit!.Value] = Math.Min(suitLengthConstraint.MaxLen,
                    knowledge.PartnerMaxShape[suitLengthConstraint.Suit!.Value]);
                
                
                return knowledge;
            
            // case PartnerKnowledgeConstraint partnerKnowledgeConstraint:
            //     if (knowledge.PartnerKnowledgeOfMe == null)
            //         return knowledge;
            //     var partnershipKnowledgeOfMe = knowledge.PartnerKnowledgeOfMe;
            //
            //     partnerKnowledgeConstraint.Requirements.TryGetValue("fit_in_suit", out var suit);
            //     if (suit != null && suit != "any")
            //     {
            //         var s = suit.ToSuit();
            //         var min = 8 - partnershipKnowledgeOfMe.PartnerMinShape[s];
            //         knowledge.PartnerMinShape[s] = Math.Max(min, knowledge.PartnerMinShape[s]);
            //     }
            //     
            //     partnerKnowledgeConstraint.Requirements.TryGetValue("combined_hcp", out var hcp);
            //     if (hcp != null)
            //     {
            //         knowledge.PartnerHcpMin = StringParser.ParseMinimum(hcp) - partnershipKnowledgeOfMe.PartnerHcpMin;
            //     }
            //     
            //     
                

                return knowledge;
                
                
            default:
                return knowledge;
            
        }
    }
}