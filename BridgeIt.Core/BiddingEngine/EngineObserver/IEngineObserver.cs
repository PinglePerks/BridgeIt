using System.Text.Json;
using System.Text.Json.Serialization;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.EngineObserver;

public interface IEngineObserver
{
    // Called when a rule is checked but found not applicable
    void OnRuleSkipped(string ruleName, DecisionContext context);

    // Called when a rule matches and produces a bid
    void OnRuleApplied(string ruleName, Bid bid, DecisionContext context);

    // Called if no rules match (Pass fallback)
    void OnNoRuleMatched(DecisionContext context);

    void PrintHands(Seat seat, Hand hand);

    /// <summary>
    /// Called after a bid decision is complete with the full evaluation trace.
    /// Default implementation is a no-op — only SignalREngineObserver overrides this.
    /// </summary>
    void OnBidDecisionComplete(RuleEvaluationLog log) { }
}

public class EngineObserver : IEngineObserver
{
    private readonly StreamWriter _writer;
    private readonly JsonSerializerOptions _options;

    public EngineObserver(string filePath = "/Users/mattyperky/RiderProjects/BridgeIt/BridgeIt.TestHarness/result.json")
    {
        _writer = new StreamWriter(filePath, append: false);
        _options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        _writer.WriteLine("["); // Start JSON array
    }

    public void PrintHands(Seat seat, Hand hand)
    {
        var str = new Dictionary<Seat, string>{{seat, hand.ToString()}};
        var json = JsonSerializer.Serialize(str, _options);
        _writer.WriteLine(json + ",");
        _writer.Flush();
    }
    public void OnRuleSkipped(string ruleName, DecisionContext context)
    {
        throw new NotImplementedException();
    }

    public void OnRuleApplied(string ruleName, Bid bid, DecisionContext ctx)
    {
        var logEntry = new
        {
            RuleName = ruleName,
            Bid = bid,
            Hand = ctx.Data.Hand.ToString(),
            HandEvaluation = new
            {
                ctx.HandEvaluation.Hcp,
                ctx.HandEvaluation.Shape,
            },
            AuctionEvaluation = new
            {
                ctx.AuctionEvaluation.SeatRoleType,
                ctx.AuctionEvaluation.PartnerLastBid,
                ctx.AuctionEvaluation.AuctionPhase,
                ctx.AuctionEvaluation.BiddingRound
            },
            TableKnowledge = ctx.TableKnowledge,
        };

        var json = JsonSerializer.Serialize(logEntry, _options);
        _writer.WriteLine(json + ",");
        _writer.Flush();
    }

    public void OnNoRuleMatched(DecisionContext ctx)
    {
        var logEntry = new
        {
            Rule = "NO RULE MATCHED",
            Hand = ctx.Data.Hand.ToString(),
            HandEvaluation = new
            {
                ctx.HandEvaluation.Hcp,
                ctx.HandEvaluation.Shape,
            },
            AuctionEvaluation = new
            {
                ctx.AuctionEvaluation.SeatRoleType,
            },
            TableKnowledge = ctx.TableKnowledge,
        };
        
        var json = JsonSerializer.Serialize(logEntry, _options);
        _writer.WriteLine(json + ",");
        _writer.Flush();
        
    }
    
    public void Dispose()
    {
        _writer.WriteLine("]"); // End JSON array
        _writer.Dispose();
    }
}