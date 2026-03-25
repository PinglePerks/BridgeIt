using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.IBidValidityChecker;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;
    private readonly IEngineObserver _observer;
    private readonly IBidValidityChecker _validityChecker;

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

            var reqs = rule.GetForwardConstraints(ctx.AuctionEvaluation);
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
    
    public BiddingEngine(IEnumerable<IBiddingRule> rules, ILogger<BiddingEngine> logger, IEngineObserver observer, IBidValidityChecker? validityChecker = null)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
        _logger = logger;
        _observer = observer;
        _validityChecker = validityChecker ?? new BidValidityChecker();
    }

    public BidResult ChooseBid(DecisionContext ctx)
    {
        _observer.PrintHands(ctx.Data.Seat, ctx.Data.Hand);

        var evaluatedRules = new List<RuleEvaluation>();

        foreach (var rule in _rules)
        {
            var eval = new RuleEvaluation
            {
                RuleName = rule.Name,
                Priority = rule.Priority,
                IsApplicableToAuction = rule.IsApplicableToAuction(ctx.AuctionEvaluation),
            };

            // Serialize forward constraints for auction-applicable rules
            if (eval.IsApplicableToAuction)
            {
                var fwd = rule.GetForwardConstraints(ctx.AuctionEvaluation);
                if (fwd != null && fwd.Constraints.Count > 0)
                {
                    eval.ForwardConstraints = fwd.Constraints
                        .Select(ConstraintSerializer.Serialize).ToList();
                }
            }

            if (!rule.CouldMakeBid(ctx))
            {
                eval.IsHandApplicable = false;

                // Evaluate constraints to show WHY it failed
                if (eval.IsApplicableToAuction)
                {
                    var fwd = rule.GetForwardConstraints(ctx.AuctionEvaluation);
                    if (fwd != null && fwd.Constraints.Count > 0)
                    {
                        eval.ConstraintResults = ConstraintSerializer.EvaluateComposite(fwd, ctx);
                    }
                }

                evaluatedRules.Add(eval);
                continue;
            }

            eval.IsHandApplicable = true;
            var decision = rule.Apply(ctx);

            if (decision == null)
            {
                evaluatedRules.Add(eval);
                continue;
            }

            eval.ProducedBid = decision.ToString();

            // Pass is always valid — skip the checker
            if (decision.Type == BidType.Pass)
            {
                eval.WasSelected = true;
                evaluatedRules.Add(eval);
                _observer.OnRuleApplied(rule.Name, decision, ctx);
                _observer.OnBidDecisionComplete(BuildLog(ctx, evaluatedRules, decision));
                return new BidResult(decision, rule.IsAlertable);
            }

            // Validate the bid is legal (higher than current contract, etc.)
            var auctionBid = new AuctionBid(ctx.Data.Seat, decision);
            if (_validityChecker.IsValid(auctionBid, ctx.Data.AuctionHistory))
            {
                eval.WasSelected = true;
                evaluatedRules.Add(eval);
                _observer.OnRuleApplied(rule.Name, decision, ctx);
                _observer.OnBidDecisionComplete(BuildLog(ctx, evaluatedRules, decision));
                return new BidResult(decision, rule.IsAlertable);
            }

            // Rule produced an illegal bid — log and try next rule
            eval.WasInvalidBid = true;
            evaluatedRules.Add(eval);
            _logger.LogWarning(
                "Rule '{Rule}' produced illegal bid {Bid} for {Seat} — skipping to next rule",
                rule.Name, decision, ctx.Data.Seat);
        }

        _observer.OnNoRuleMatched(ctx);
        _observer.OnBidDecisionComplete(BuildLog(ctx, evaluatedRules, Bid.Pass()));
        return new BidResult(Bid.Pass());
    }

    private static RuleEvaluationLog BuildLog(DecisionContext ctx, List<RuleEvaluation> evaluatedRules, Bid winningBid)
    {
        return new RuleEvaluationLog
        {
            Seat = ctx.Data.Seat.ToString(),
            Hand = ctx.Data.Hand.ToString(),
            Hcp = ctx.HandEvaluation.Hcp,
            IsBalanced = ctx.HandEvaluation.IsBalanced,
            Shape = ctx.HandEvaluation.Shape.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            SeatRole = ctx.AuctionEvaluation.SeatRoleType.ToString(),
            AuctionPhase = ctx.AuctionEvaluation.AuctionPhase.ToString(),
            BiddingRound = ctx.AuctionEvaluation.BiddingRound,
            PartnerLastBid = ctx.AuctionEvaluation.PartnerLastBid?.ToString() ?? "—",
            TableKnowledge = ctx.TableKnowledge.Players.ToDictionary(
                kv => kv.Key.ToString(),
                kv => new TableKnowledgeEntry
                {
                    HcpMin = kv.Value.HcpMin,
                    HcpMax = kv.Value.HcpMax,
                    IsBalanced = kv.Value.IsBalanced,
                    MinShape = kv.Value.MinShape.ToDictionary(s => s.Key.ToString(), s => s.Value),
                    MaxShape = kv.Value.MaxShape.ToDictionary(s => s.Key.ToString(), s => s.Value),
                }),
            WinningBid = winningBid.ToString(),
            EvaluatedRules = evaluatedRules,
        };
    }
}