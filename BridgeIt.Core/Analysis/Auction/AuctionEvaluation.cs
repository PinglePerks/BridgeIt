using System.Threading.Tasks.Dataflow;
using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

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
    public static AuctionEvaluation Evaluate(AuctionHistory auctionHistory, Seat seat)
    {
        
        return new AuctionEvaluation()
        {
            CurrentContract = auctionHistory.Bids
                .LastOrDefault(x => x.ChosenBid.Type == BidType.NoTrumps || x.ChosenBid.Type == BidType.Suit)?
                .ChosenBid,
            SeatRole = GetSeatRole(auctionHistory, seat),
            PartnershipState = GetPartnershipState(auctionHistory),
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
    public static PartnershipKnowledge AnalyzeKnowledge(AuctionHistory history, Seat mySeat, Domain.Primatives.Hand myHand)
    {
        var knowledge = new PartnershipKnowledge();

        var partnersKnowledgeOfMe = new PartnershipKnowledge();

        var myBids = history.GetAllPartnerBids((Seat)mySeat + 2 % 4);
        foreach (var bid in myBids)
            if (bid.AppliedConstraint != null)
                partnersKnowledgeOfMe = ExtractKnowledgeFromConstraint(bid.AppliedConstraint, partnersKnowledgeOfMe);
        
        

        var partnerBids = history.GetAllPartnerBids(mySeat);
        
        foreach (var bid in partnerBids)
            if (bid.AppliedConstraint != null)
                knowledge = ExtractKnowledgeFromConstraint(bid.AppliedConstraint, knowledge, partnersKnowledgeOfMe);

        var handShape = ShapeEvaluator.GetShape(myHand);
        
        knowledge.FitInSuit = knowledge.BestFitSuit(handShape);
        
        
        return knowledge;
    }

    public static PartnershipKnowledge ExtractKnowledgeFromConstraint(IBidConstraint constraint, PartnershipKnowledge knowledge, PartnershipKnowledge? partnershipKnowledgeOfMe = null)
    {
        switch (constraint)
        {
            case CompositeConstraint composite:

                foreach (var child in composite.Constraints)
                {
                    knowledge = ExtractKnowledgeFromConstraint(child, knowledge, partnershipKnowledgeOfMe);
                }
                return knowledge;
            
            case HcpConstraint hcpConstraint:
                knowledge.PartnerHcpMax = Math.Min(knowledge.PartnerHcpMax, hcpConstraint.Max);
                
                knowledge.PartnerHcpMin = Math.Max(knowledge.PartnerHcpMin, hcpConstraint.Min);
                return knowledge;
            
            case BalancedConstraint balancedConstraint:
                knowledge.PartnerIsBalanced = true;
                
                return knowledge;
            
            case SuitLengthConstraint suitLengthConstraint:
                if (suitLengthConstraint.Suit == null) return knowledge;

                knowledge.PartnerMinShape[suitLengthConstraint.Suit!.Value] = Math.Max(suitLengthConstraint.MinLen,
                    knowledge.PartnerMinShape[suitLengthConstraint.Suit!.Value]);

                knowledge.PartnerMaxShape[suitLengthConstraint.Suit!.Value] = Math.Min(suitLengthConstraint.MaxLen,
                    knowledge.PartnerMaxShape[suitLengthConstraint.Suit!.Value]);
                
                
                
                
                
                return knowledge;
            
            case PartnerKnowledgeConstraint partnerKnowledgeConstraint:
                if (partnershipKnowledgeOfMe == null)
                    return knowledge;

                partnerKnowledgeConstraint.Requirements.TryGetValue("fit_in_suit", out var suit);
                if (suit == null) return knowledge;
                var s = suit.ToSuit();
                
                var min = 8 - partnershipKnowledgeOfMe.PartnerMinShape[s];
                knowledge.PartnerMinShape[s] = Math.Max(min, knowledge.PartnerMinShape[s]);

                return knowledge;
                
                
            default:
                return knowledge;
            
        }
    }

    public static Bid? PartnerLastBid(AuctionHistory history)
    {
        if (history.Bids.Count < 2) return null;
        return history.Bids[^2].ChosenBid;
    }

    private static string GetPartnershipState(AuctionHistory auctionHistory)
    {
        if (auctionHistory.Bids.All(a => a.ChosenBid.Type == BidType.Pass)) return "opening";
        if (auctionHistory.Bids.Count < 2) return "opening";
            return auctionHistory.Bids[^2].NextPartnershipState;
    }
}

public enum SeatRole
{
    NoBids,
    Opener,
    Responder,
    Overcaller
}