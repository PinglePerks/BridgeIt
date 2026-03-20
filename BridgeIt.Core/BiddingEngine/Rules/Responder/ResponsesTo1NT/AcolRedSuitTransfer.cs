// using BridgeIt.Core.Analysis.Auction;
// using BridgeIt.Core.BiddingEngine.Constraints;
// using BridgeIt.Core.BiddingEngine.Core;
// using BridgeIt.Core.Domain.Bidding;
//
// namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
//
// public class AcolRedSuitTransfer: BiddingRuleBase
// {
//     public override string Name { get; } = "Red Suit Transfer";
//     public override int Priority { get; } = 30; // Higher priority than a standard suit opening
//
//     public override bool CouldMakeBid(DecisionContext ctx)
//     {
//         if (ctx.AuctionEvaluation.SeatRoleType != SeatRoleType.NoBids)
//             return false;
//
//         return ctx.HandEvaluation.Hcp is >= MinHcp and <= MaxHcp 
//                && ctx.HandEvaluation.IsBalanced;
//     }
//
//     public override Bid? Apply(DecisionContext ctx)
//     {
//         return Bid.NoTrumpsBid(1);
//     }
//
//     public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
//     {
//         if (ctx.AuctionEvaluation.CurrentContract != null) return false;
//         
//         return bid is { Type: BidType.NoTrumps, Level: 2 };
//     }
//
//     public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
//     {
//         var constraints = new CompositeConstraint();
//         constraints.Add(new HcpConstraint(MinHcp, MaxHcp));
//         constraints.Add(new BalancedConstraint()); // Assuming you have this!
//         
//         return new BidInformation(bid, constraints, null);
//     }
// }