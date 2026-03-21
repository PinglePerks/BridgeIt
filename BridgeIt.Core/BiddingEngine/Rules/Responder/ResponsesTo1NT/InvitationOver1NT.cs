// using BridgeIt.Core.BiddingEngine.Core;
// using BridgeIt.Core.Domain.Bidding;
//
// namespace BridgeIt.Core.BiddingEngine.Rules.Responder.ResponsesTo1NT;
//
// public class InvitationOver1NT : BiddingRuleBase
// {
//     public override string Name { get; } = "Invitation over 1NT";
//     public override int Priority { get; } = 28;
//     pri
//     public override bool CouldMakeBid(DecisionContext ctx)
//     {
//         if (ctx.PartnershipKnowledge.PartnershipBiddingState != PartnershipBiddingState.ConstructiveSearch)
//             return false;
//         
//         if (ctx.AuctionEvaluation.CurrentContract != ApplicableOpeningBid) return false;
//         
//         if (ctx.AuctionEvaluation.BiddingRound != 1) return false;
//         
//         return ctx.HandEvaluation.Shape[Suit.Hearts] >= 4 || ctx.HandEvaluation.Shape[Suit.Spades] >= 4 && ctx.HandEvaluation.Hcp >= HcpMin;
//         
//         
//     }
//
//     public override Bid? Apply(DecisionContext ctx)
//     {
//         throw new NotImplementedException();
//     }
//
//     public override bool CouldExplainBid(Bid bid, DecisionContext ctx)
//     {
//         throw new NotImplementedException();
//     }
//
//     public override BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx)
//     {
//         throw new NotImplementedException();
//     }
// }