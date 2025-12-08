using System.Threading.Tasks.Dataflow;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Domain.Utilities;

namespace BridgeIt.Core.Analysis.Auction;

public class AuctionEvaluation
{
    public SeatRole SeatRole { get; init; }
    // public bool IsCompetition { get; init; }
    // public bool IsForcing { get; init; }
    // public Suit? AgreedSuit { get; init; }
    // public bool HasAgreedSuit { get; init; }
    public Suit? SuitFit {get; init;}
    public Bid? CurrentContract { get; init; }
    public Bid? PartnerLastBid { get; init; }
    public string PartnershipState { get; init; }
}

public static class AuctionEvaluator
{
    public static AuctionEvaluation Evaluate(AuctionHistory auctionHistory, Seat seat, string nextState)
    {
        
        return new AuctionEvaluation()
        {
            CurrentContract = auctionHistory.Bids
                .LastOrDefault(x => x.Decision.ChosenBid.Type == BidType.NoTrumps || x.Decision.ChosenBid.Type == BidType.Suit)?
                .Decision.ChosenBid,
            SeatRole = GetSeatRole(auctionHistory, seat),
            PartnershipState = nextState,
            PartnerLastBid = PartnerLastBid(auctionHistory),
        };
    }

    public static SeatRole GetSeatRole(AuctionHistory auctionHistory, Seat currentSeat)
    {
        var openingSeat = auctionHistory.OpeningBidder();
        if (openingSeat == null) return SeatRole.NoBids;
        
        if (openingSeat == currentSeat) return SeatRole.Opener;
        
        var difference = ((int)currentSeat - (int)openingSeat + 4) % 4;

        return difference switch
        {
            1 => SeatRole.Overcaller,
            2 => SeatRole.Responder,
            3 => SeatRole.Overcaller,
            _ => throw new ArgumentOutOfRangeException()
        };
        
    }
    
    public static PartnershipKnowledge AnalyzeKnowledge(List<IBidConstraint> bidConstraints)
    {
        var knowledge = new PartnershipKnowledge();

        foreach (var bidConstraint in bidConstraints)
        {
            
            knowledge = ExtractKnowledgeFromConstraint(bidConstraint, knowledge);
        }
        
        return knowledge;
    }
    
    public static PartnershipKnowledge AnalyzeKnowledgeOfMe(AuctionHistory history, Seat mySeat, Hand myHand, PartnershipKnowledge knowledge, Dictionary<Seat, List<IBidConstraint>> bidConstraints)
    {
        var partnerBids = history.GetAllPartnerBids(mySeat);

        var partnerSeatIndex = ((int)mySeat + 2) % 4;

        var partnerSeat = (Seat)partnerSeatIndex;
        
        var partnerConstraints = bidConstraints[partnerSeat];
        
        foreach (var c in partnerConstraints)
            knowledge = ExtractKnowledgeFromConstraint(c, knowledge);

        var handShape = ShapeEvaluator.GetShape(myHand);
        
        knowledge.FitInSuit = knowledge.BestFitSuit(handShape);
        
        
        return knowledge;
    }
    
    public static PartnershipKnowledge ExtractKnowledgeFromConstraint(IBidConstraint constraint, PartnershipKnowledge knowledge)
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
            
            case PartnerKnowledgeConstraint partnerKnowledgeConstraint:
                if (knowledge.PartnerKnowledgeOfMe == null)
                    return knowledge;
                var partnershipKnowledgeOfMe = knowledge.PartnerKnowledgeOfMe;

                partnerKnowledgeConstraint.Requirements.TryGetValue("fit_in_suit", out var suit);
                if (suit != null && suit != "any")
                {
                    var s = suit.ToSuit();
                    var min = 8 - partnershipKnowledgeOfMe.PartnerMinShape[s];
                    knowledge.PartnerMinShape[s] = Math.Max(min, knowledge.PartnerMinShape[s]);
                }
                
                partnerKnowledgeConstraint.Requirements.TryGetValue("combined_hcp", out var hcp);
                if (hcp != null)
                {
                    knowledge.PartnerHcpMin = StringParser.ParseMinimum(hcp) - partnershipKnowledgeOfMe.PartnerHcpMin;
                }
                
                
                

                return knowledge;
                
                
            default:
                return knowledge;
            
        }
    }

    public static Bid? PartnerLastBid(AuctionHistory history)
    {
        if (history.Bids.Count < 2) return null;
        return history.Bids[^2].Decision.ChosenBid;
    }

    private static string GetPartnershipState(AuctionHistory auctionHistory)
    {
        if (auctionHistory.Bids.All(a => a.Decision.ChosenBid.Type == BidType.Pass)) return "opening";
        if (auctionHistory.Bids.Count < 2) return "opening";
            return auctionHistory.Bids[^2].Decision.NextPartnershipState;
    }
}

public enum SeatRole
{
    NoBids,
    Opener,
    Responder,
    Overcaller
}