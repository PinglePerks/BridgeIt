using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Core;

public abstract class BiddingRuleBase : IBiddingRule
{
    public abstract int Priority { get; }
    public abstract bool IsApplicable(BiddingContext ctx);
    public abstract BiddingDecision? Apply(BiddingContext ctx);

    protected int Hcp(Hand hand) => HighCardPoints.Count(hand);

    protected bool IsBalanced(Hand hand) => ShapeEvaluator.IsBalanced(hand);
    
    protected Suit LongestAndStrongest(Hand hand) => hand.Cards.GroupBy(c => c.Suit).OrderByDescending(g => g.Count()).First().Key;
    
    protected bool AllPassed(IReadOnlyList<Bid> bids) => bids.All(b => b.Type == BidType.Pass);

    protected int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
    {
        if (currentContract == null) return 1;
        var level = currentContract.Level;
        if (currentContract.Type == BidType.NoTrumps) return level + 1;
        if (suit <= currentContract.Suit) return level + 1;
        return level;
    }

    protected int GetNextNtBidLevel(Bid? currentContract)
    {
        if (currentContract == null) return 1;
        if (currentContract.Type == BidType.NoTrumps) return currentContract.Level + 1;
        return currentContract.Level;
    }
}

public class ResponseTo2ntOpening : BiddingRuleBase
{
    public override int Priority => 100;
    public override bool IsApplicable(BiddingContext ctx) 
        => ctx.AuctionEvaluation.PartnershipState == "responses_to_2nt_opening";

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 5)
        {
            return new BiddingDecision(Bid.SuitBid(3, Suit.Diamonds), "Transfer to hearts", "red_suit_transfer",
                new SuitLengthConstraint("hearts", ">=5"));
        }
        if (ctx.HandEvaluation.Shape[Suit.Spades] >= 5)
        {
            return new BiddingDecision(Bid.SuitBid(3, Suit.Hearts), "Transfer to spades", "red_suit_transfer",
                new SuitLengthConstraint("spades", ">=5"));
        } 
        return null;
    }
}

// public class RespondingToNaturalOpening : BiddingRuleBase
// {
//     public override int Priority => 100;
//
//     public override bool IsApplicable(BiddingContext ctx)
//         => ctx.AuctionEvaluation.PartnershipState == "natural_response";
//
//     public override BiddingDecision? Apply(BiddingContext ctx)
//     {
//         var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
//
//         if (partnerBid == null || partnerBid.Type == BidType.Pass)
//         {
//             return null;
//         }
//         
//         if (ctx.PartnershipKnowledge.HasFit(partnerBid.Suit!.Value, ctx.HandEvaluation.Shape[partnerBid.Suit.Value]))
//         {
//             
//         }
//
//         return null;
//     }
//
//     protected internal BiddingDecision? ResponseToFit(Suit suit, BiddingContext ctx)
//     {
//         var expPartnerLosers = 7;
//
//         if (ctx.HandEvaluation.Losers <= 7)
//         {
//             
//         }
//
//         return null;
//     }
//         
// }

public class MajorFitWithPartner : BiddingRuleBase
{
    public override int Priority => 100;

    public override bool IsApplicable(BiddingContext ctx)
    {
        if(ctx.AuctionEvaluation.PartnershipState != "natural_response") return false;
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        if(partnerBid == null || partnerBid.Suit == null) return false;
        return ctx.PartnershipKnowledge.HasFit(partnerBid.Suit!.Value, ctx.HandEvaluation.Shape[partnerBid.Suit.Value]);
    }

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        var nextBidLevel = GetNextSuitBidLevel(ctx.HandEvaluation.Shape.OrderByDescending(s => s.Value).First().Key,
            ctx.AuctionEvaluation.CurrentContract);
        var fitSuit = ctx.AuctionEvaluation.PartnerLastBid!.Suit;
        var hcp = ctx.HandEvaluation.Hcp;
        var losers = ctx.HandEvaluation.Losers;

        if (hcp < 6) return new BiddingDecision(Bid.Pass(), "weak - under 6 hcp", "passed", new HcpConstraint("<=6"));

        if (nextBidLevel == 2)
        {
            if (hcp < 10 || losers > 8)
                return new BiddingDecision(Bid.SuitBid(nextBidLevel, fitSuit!.Value), "weak - under 10 hcp",
                    "major_fit",null, true);
        }
        else
        {
            if (hcp < 10 || losers > 8)
                return new BiddingDecision(Bid.Pass(), "weak - under 6 hcp", "passed", new HcpConstraint("<=10"));
        }

        if (nextBidLevel <= 3)
        {
            if (losers == 8)
                return new BiddingDecision(Bid.SuitBid(3, fitSuit!.Value), "limited raise - not quite enough for game",
                    "major_fit",null, true);
        }

        if (losers <= 7)
        {
            return new BiddingDecision(Bid.SuitBid(4, fitSuit!.Value), "Major fit + enough points for game",
                "major_fit",null, true);
        }

        return new BiddingDecision(Bid.Pass(), "too high level to bid", "passed", new HcpConstraint("<=12"));
    }
}

public class RedSuitTransfer : BiddingRuleBase
{
    public override int Priority => 100;
    public override bool IsApplicable(BiddingContext ctx) 
        => ctx.AuctionEvaluation.PartnershipState == "red_suit_transfer";

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
        var level = partnerBid!.Level!;
        var suit = partnerBid.Suit!;

        var transfer = (int)suit + 1;
        if (transfer == 4)
        {
            transfer = 0 ;
            level++;
        }
        
        return new BiddingDecision(Bid.SuitBid(level, (Suit)transfer), "compulsory correction", "transferred",null);
    }
    
}

public class DefaultBidding : BiddingRuleBase
{
    public override int Priority => 1;

    public override bool IsApplicable(BiddingContext ctx)
        => ctx.AuctionEvaluation.SeatType == SeatRole.Responder;

    public override BiddingDecision? Apply(BiddingContext ctx)
    {
        //TODO; check if already agreed a fit?
        
        var suit = ctx.PartnershipKnowledge.BestFitSuit(ctx.HandEvaluation.Shape);

        var lowestHcp = ctx.PartnershipKnowledge.PartnerHcpMin;

        if (suit == null)
        {
            var totalHcp = lowestHcp + ctx.HandEvaluation.Hcp;
            if (totalHcp >= 25) return new BiddingDecision(Bid.NoTrumpsBid(3), "No suit fit", "no_suit_fit");
            
            if(totalHcp >= 22) return new BiddingDecision(Bid.NoTrumpsBid(2), "No Suit fit", "no_suit_fit");
        }
        else
        {
            
        }

        return null;
    }
    
}
