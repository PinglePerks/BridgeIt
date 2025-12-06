// using BridgeIt.Core.BiddingEngine.Constraints;
// using BridgeIt.Core.Domain.Bidding;
// using BridgeIt.Core.Domain.Primatives;
//
// namespace BridgeIt.Core.BiddingEngine.Core;
//
// public class ResponseTo2ntOpening : BiddingRuleBase
// {
//     public override string Name => "Codebased--->Response To 2NT Opening";
//     public override int Priority => 100;
//     public override bool IsApplicable(BiddingContext ctx) 
//         => ctx.AuctionEvaluation.PartnershipState == "responses_to_2nt_opening";
//
//     public override BiddingDecision? Apply(BiddingContext ctx)
//     {
//         if (ctx.HandEvaluation.Shape[Suit.Hearts] >= 5)
//         {
//             return new BiddingDecision(Bid.SuitBid(3, Suit.Diamonds), "Transfer to hearts", "transfer",
//                 new SuitLengthConstraint("hearts", ">=5"));
//         }
//         if (ctx.HandEvaluation.Shape[Suit.Spades] >= 5)
//         {
//             return new BiddingDecision(Bid.SuitBid(3, Suit.Hearts), "Transfer to spades", "transfer",
//                 new SuitLengthConstraint("spades", ">=5"));
//         } 
//         return null;
//     }
// }