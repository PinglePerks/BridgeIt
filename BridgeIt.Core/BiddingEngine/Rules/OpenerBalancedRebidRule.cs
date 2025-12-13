// using System.ComponentModel;
// using BridgeIt.Core.Analysis.Auction;
// using BridgeIt.Core.BiddingEngine.Constraints;
// using BridgeIt.Core.BiddingEngine.Core;
// using BridgeIt.Core.Domain.Bidding;
// using BridgeIt.Core.Domain.Primatives;
//
// namespace BridgeIt.Core.BiddingEngine.Rules;
//
// public class OpenerBalancedRebidRule : BiddingRuleBase
// {
//     public override string Name { get; } = "Codebased---Opener Balanced Rebid";
//     public override int Priority { get; } = 25;
//     public override bool IsApplicable(BiddingContext ctx)
//     {
//         var opener = ctx.AuctionEvaluation.SeatRole == SeatRole.Opener;
//         var secondBid = ctx.AuctionHistory.GetAllSeatBids(ctx.Seat).Count() == 1;
//         var balanced = ctx.HandEvaluation.IsBalanced;
//         return opener && secondBid && balanced;
//
//     }
//
//     public override IBidConstraint? GetConstraintForBid(Bid bid, BiddingContext ctx)
//     {
//         throw new NotImplementedException();
//     }
//
//     public override BiddingDecision? Apply(BiddingContext ctx)
//     {
//         var currentContract = ctx.AuctionEvaluation.CurrentContract;
//
//         var level = GetNextNtBidLevel(currentContract);
//         
//         var hcp = ctx.HandEvaluation.Hcp;
//
//         if (level == 1)
//         {
//             if (hcp >= 15 && hcp <= 17)
//             {
//                 return new BiddingDecision(Bid.NoTrumpsBid(1), "balanced / 15-17 hcp", "natural", new HcpConstraint("15-17"));
//             }
//             return new BiddingDecision(Bid.NoTrumpsBid(2), "balanced / 18-19 hcp", "natural", new HcpConstraint("18-19"));
//         }
//
//         if (level == 2)
//         {
//             return new BiddingDecision(Bid.NoTrumpsBid(2), "balanced / 15-19 hcp", "natural", new HcpConstraint("15-19"));
//
//         }
//         
//         return null;
//     }
//
// }