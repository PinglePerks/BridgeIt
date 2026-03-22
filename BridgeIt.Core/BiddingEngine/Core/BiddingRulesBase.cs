using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Core;

public abstract class BiddingRuleBase : IBiddingRule
{
    public abstract string Name { get; }
    public abstract int Priority { get; }

    /// <summary>
    /// Checks whether this rule is applicable given only the auction state.
    /// Called by both CouldMakeBid (forward) and CouldExplainBid (backward)
    /// to eliminate duplication. Takes AuctionEvaluation only — no hand data,
    /// so backward inference cannot accidentally access private information.
    /// </summary>
    protected virtual bool IsApplicableContext(AuctionEvaluation auction) => true;

    /// <summary>
    /// Forward: checks auction context via IsApplicableContext, then hand requirements.
    /// Subclasses override IsHandApplicable for hand-specific checks.
    /// </summary>
    public virtual bool CouldMakeBid(DecisionContext ctx)
    {
        if (!IsApplicableContext(ctx.AuctionEvaluation)) return false;
        return IsHandApplicable(ctx);
    }

    /// <summary>
    /// Forward: given the hand, does it meet this rule's requirements?
    /// Only called when IsApplicableContext has already passed.
    /// </summary>
    protected virtual bool IsHandApplicable(DecisionContext ctx) => true;

    /// <summary>
    /// Backward: checks auction context via IsApplicableContext, then bid shape.
    /// Subclasses override IsBidExplainable for bid-matching checks.
    /// </summary>
    public virtual bool CouldExplainBid(Bid bid, DecisionContext ctx)
    {
        if (!IsApplicableContext(ctx.AuctionEvaluation)) return false;
        return IsBidExplainable(bid, ctx);
    }

    /// <summary>
    /// Backward: could this bid have been produced by this rule?
    /// Takes full DecisionContext so rules can query PartnershipKnowledge
    /// (accumulated from replayed auction — all public information).
    /// Hand evaluation will be blank when inferring about other players.
    /// Only called when IsApplicableContext has already passed.
    /// </summary>
    protected virtual bool IsBidExplainable(Bid bid, DecisionContext ctx) => false;

    public abstract BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx);
    public abstract Bid? Apply(DecisionContext ctx);

    protected int GetNextSuitBidLevel(Suit suit, Bid? currentContract)
        => Bid.NextLevelForSuit(suit, currentContract);

    protected int GetNextNtBidLevel(Bid? currentContract)
        => Bid.NextLevelForNoTrumps(currentContract);
}
