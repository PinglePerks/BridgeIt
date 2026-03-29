using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Api.Services;

/// <summary>
/// An IEngineObserver that collects rule evaluation logs in memory
/// for use in match analysis (synchronous auction replay).
/// </summary>
public class CollectingEngineObserver : IEngineObserver
{
    public List<RuleEvaluationLog> Logs { get; } = new();

    public void OnRuleSkipped(string ruleName, DecisionContext context) { }

    public void OnRuleApplied(string ruleName, Bid bid, DecisionContext context) { }

    public void OnNoRuleMatched(DecisionContext context) { }

    public void PrintHands(Seat seat, Hand hand) { }

    public void OnBidDecisionComplete(RuleEvaluationLog log)
    {
        Logs.Add(log);
    }
}
