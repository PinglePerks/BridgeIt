using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Domain.Bidding;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;
    private readonly IEngineObserver _observer;

    public BidInformation GetConstraintsFromBid(DecisionContext decisionContext, Bid bid)
    {
        // Pass — try rule-based negative inference first
        if (bid.Type == BidType.Pass)
        {
            // First check if any rule explicitly explains what this pass means
            // (e.g. AcolNTRaiseOver1NT returns 0-10 HCP for a pass after 1NT)
            foreach (var rule in _rules)
            {
                if (rule.CouldExplainBid(bid, decisionContext))
                {
                    var bidInfo = rule.GetConstraintForBid(bid, decisionContext);
                    if (bidInfo != null)
                        return bidInfo;
                }
            }

            // No rule explicitly handles this pass — use negative inference
            // from the applicable rules that COULD have fired but didn't.
            return InferFromPass(decisionContext)
                   ?? new BidInformation(bid, null, PartnershipBiddingState.Unknown);
        }

        // Non-pass — existing rule-matching logic
        foreach (var rule in _rules)
        {
            if (rule.CouldExplainBid(bid, decisionContext))
            {
                var bidInformation = rule.GetConstraintForBid(bid, decisionContext);
                if (bidInformation != null)
                    return bidInformation;
            }
        }

        return FallbackConstraintExtractor.Extract(bid)
               ?? new BidInformation(bid, null, PartnershipBiddingState.Unknown);
    }

    /// <summary>
    /// Negative inference from a pass: for each applicable rule, create a
    /// NegatedCompositeConstraint from its minimum forward requirements.
    /// Each negation says "the player does NOT satisfy all of rule X's
    /// requirements simultaneously." PlayerKnowledgeEvaluator resolves
    /// these against accumulated positive knowledge to derive concrete
    /// inferences (e.g. if HCP is already known, the remaining component
    /// — suit length — must be false).
    /// </summary>
    private BidInformation? InferFromPass(DecisionContext ctx)
    {
        var passConstraints = new CompositeConstraint();

        foreach (var rule in _rules)
        {
            if (!rule.IsApplicableToAuction(ctx.AuctionEvaluation))
                continue;

            var reqs = rule.GetMinimumForwardRequirements(ctx.AuctionEvaluation);
            if (reqs == null || reqs.Constraints.Count == 0)
                continue;

            var negated = new NegatedCompositeConstraint();
            foreach (var c in reqs.Constraints)
                negated.Add(c);
            passConstraints.Add(negated);
        }

        return passConstraints.Constraints.Count > 0
            ? new BidInformation(Bid.Pass(), passConstraints, PartnershipBiddingState.Unknown)
            : null;
    }
    
    public BiddingEngine(IEnumerable<IBiddingRule> rules, ILogger<BiddingEngine> logger, IEngineObserver observer)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
        _logger = logger;
        _observer = observer;
        
    }

    public Bid ChooseBid(DecisionContext ctx)
    {
        _observer.PrintHands(ctx.Data.Seat, ctx.Data.Hand);
        
        foreach (var rule in _rules)
        {
            if (!rule.CouldMakeBid(ctx))
            {
                continue;
            }
            
            var decision = rule.Apply(ctx);
            
            if (decision != null)
            {
                _observer.OnRuleApplied(rule.Name, decision, ctx);
                return decision;
            }
        }
        _observer.OnNoRuleMatched(ctx);
        return Bid.Pass();
    }
}