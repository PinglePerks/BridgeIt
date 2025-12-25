

using System.ComponentModel.DataAnnotations;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;
    private readonly IEngineObserver _observer; // <--- Add this

    public BidInformation GetConstraintsFromBid(DecisionContext decisionContext, Bid bid)
    {
        foreach (var rule in _rules)
        {
            if (rule.IsApplicable(decisionContext))
            {
                var bidInformation = rule.GetConstraintForBid(bid, decisionContext);
                if (bidInformation != null)
                    return bidInformation;
            }
        }

        return new BidInformation(bid, null, null);
    }
    
    public BiddingEngine(IEnumerable<IBiddingRule> rules, ILogger<BiddingEngine> logger, IEngineObserver observer)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
        _logger = logger;
        _observer = observer;
    }

    public Bid ChooseBid(DecisionContext ctx)
    {
        
        foreach (var rule in _rules)
        {
            if (!rule.IsApplicable(ctx))
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