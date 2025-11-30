using System.Threading.Tasks.Dataflow;
using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Analysis.Auction;

public class AuctionEvaluation
{
    public SeatRole? SeatType { get; init; }
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
            SeatType = GetSeatRole(auctionHistory, seat),
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

        var partnerBids = history.GetAllPartnerBids(mySeat);
        
        foreach (var bid in partnerBids)
            if (bid.AppliedConstraint != null)
                knowledge = ExtractKnowledgeFromConstraint(bid.AppliedConstraint, knowledge);

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
                
                return knowledge;
            
            case SuitLengthConstraint suitLengthConstraint:

                knowledge.PartnerMinShape[suitLengthConstraint.Suit] = Math.Max(suitLengthConstraint.MinLen,
                    knowledge.PartnerMinShape[suitLengthConstraint.Suit]);

                knowledge.PartnerMaxShape[suitLengthConstraint.Suit] = Math.Min(suitLengthConstraint.MaxLen,
                    knowledge.PartnerMaxShape[suitLengthConstraint.Suit]);
                
                
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
        if (auctionHistory.Bids.Last().ChosenBid.Type != BidType.Pass) return "overcalling";
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