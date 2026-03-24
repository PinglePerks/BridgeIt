using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Core;

public interface IBiddingRule
{
    //Metadata
    string Name { get; }
    int Priority { get; }

    //Forward solving
    bool CouldMakeBid(DecisionContext ctx);
    Bid? Apply(DecisionContext ctx);

    //Backward solving
    bool CouldExplainBid(Bid bid, DecisionContext ctx);
    BidInformation? GetConstraintForBid(Bid bid, DecisionContext ctx);

    /// <summary>
    /// Public check: is this rule applicable given only the auction state?
    /// Used by the engine for negative inference (what does a pass tell us?).
    /// </summary>
    bool IsApplicableToAuction(AuctionEvaluation auction);

    /// <summary>
    /// Returns the minimum hand requirements for any bid this rule could produce.
    /// Used for negative inference when a player passes — each applicable rule's
    /// requirements become a NegatedCompositeConstraint (the player does NOT
    /// satisfy all of these simultaneously).
    ///
    /// Return null for rules where negative inference is not meaningful
    /// (e.g. rebid rules where the player's range is already known, transfers
    /// with no HCP floor, or rules that already handle Pass explicitly).
    /// </summary>
    CompositeConstraint? GetForwardConstraints(AuctionEvaluation auction);
}