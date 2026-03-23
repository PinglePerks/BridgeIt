// using BridgeIt.Core.Analysis.Auction;
// using BridgeIt.Core.BiddingEngine.Constraints;
// using BridgeIt.Core.BiddingEngine.Core;
// using BridgeIt.Core.Domain.Bidding;
//
// namespace BridgeIt.Core.BiddingEngine.Rules.OpenerRebid;
//
// public class AcolRebidSuit : BiddingRuleBase
// {
//     public override string Name { get; } = "Acol suit rebid";
//     public override int Priority { get; } = 25;
//     
//     protected override bool IsApplicableContext(AuctionEvaluation auction)
//     {
//         if (auction.SeatRoleType == SeatRoleType.Opener && auction.BiddingRound == 1)
//         {
//             if (auction.OpeningBid!.Type == BidType.Suit && auction.OpeningBid.Level == 1)
//             {
//                 return true;
//             }
//         }
//
//         return false;
//     }
//
//     protected override bool IsHandApplicable(DecisionContext ctx)
//     {
//         return true;
//     }
//     public override Bid? Apply(DecisionContext ctx)
//     {
//         return null;
//     }
//     public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
//     {
//         
//         return null;
//     }
//
//
// }