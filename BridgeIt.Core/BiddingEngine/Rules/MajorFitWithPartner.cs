// using BridgeIt.Core.BiddingEngine.Constraints;
// using BridgeIt.Core.Domain.Bidding;
//
// namespace BridgeIt.Core.BiddingEngine.Core;
//
// public class MajorFitWithPartner : BiddingRuleBase
// {
//     public override string Name => "Codebased---Major fit with partner";
//     public override int Priority => 100;
//
//     public override bool IsApplicable(BiddingContext ctx)
//     {
//         if(ctx.AuctionEvaluation.PartnershipState != "natural_response") return false;
//         var partnerBid = ctx.AuctionEvaluation.PartnerLastBid;
//         if(partnerBid == null || partnerBid.Suit == null) return false;
//         return ctx.PartnershipKnowledge.HasFit(partnerBid.Suit!.Value, ctx.HandEvaluation.Shape[partnerBid.Suit.Value]);
//     }
//
//     public override IBidConstraint? GetConstraintForBid(Bid bid, BiddingContext ctx)
//     {
//         throw new NotImplementedException();
//     }
//
//     public override BiddingDecision? Apply(BiddingContext ctx)
//     {
//         var nextBidLevel = GetNextSuitBidLevel(ctx.HandEvaluation.Shape.OrderByDescending(s => s.Value).First().Key,
//             ctx.AuctionEvaluation.CurrentContract);
//         var fitSuit = ctx.AuctionEvaluation.PartnerLastBid!.Suit;
//         var hcp = ctx.HandEvaluation.Hcp;
//         var losers = ctx.HandEvaluation.Losers;
//
//         if (hcp < 6) return new BiddingDecision(Bid.Pass(), "weak - under 6 hcp", "passed", new HcpConstraint("<=6"));
//
//         if (nextBidLevel == 2)
//         {
//             if (hcp < 10 || losers > 8)
//                 return new BiddingDecision(Bid.SuitBid(nextBidLevel, fitSuit!.Value), "weak - under 10 hcp",
//                     "major_fit",null, true);
//         }
//         else
//         {
//             if (hcp < 10 || losers > 8)
//                 return new BiddingDecision(Bid.Pass(), "weak - under 6 hcp", "passed", new HcpConstraint("<=10"));
//         }
//
//         if (nextBidLevel <= 3)
//         {
//             if (losers == 8)
//                 return new BiddingDecision(Bid.SuitBid(3, fitSuit!.Value), "limited raise - not quite enough for game",
//                     "major_fit",null, true);
//         }
//
//         if (losers <= 7)
//         {
//             return new BiddingDecision(Bid.SuitBid(4, fitSuit!.Value), "Major fit + enough points for game",
//                 "major_fit",null, true);
//         }
//
//         return new BiddingDecision(Bid.Pass(), "too high level to bid", "passed", new HcpConstraint("<=12"));
//     }
// }