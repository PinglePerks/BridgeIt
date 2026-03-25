using BridgeIt.Api.Hubs;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.EngineObserver;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using Microsoft.AspNetCore.SignalR;


namespace BridgeIt.Api.Services;

/// <summary>
/// Broadcasts bidding engine debug events to the UI via SignalR.
/// Replaces the file-based EngineObserver for the web API.
/// </summary>
public class SignalREngineObserver : IEngineObserver
{
    private readonly IHubContext<GameHub> _hubContext;

    public SignalREngineObserver(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void OnRuleApplied(string ruleName, Bid bid, DecisionContext ctx)
    {
        var payload = BuildPayload(ruleName, bid.ToString(), ctx);
        // Fire-and-forget: observer is synchronous by interface contract
        _ = _hubContext.Clients.All.SendAsync("BidDebug", payload);
    }

    public void OnNoRuleMatched(DecisionContext ctx)
    {
        var payload = BuildPayload("NO RULE MATCHED", "Pass", ctx);
        _ = _hubContext.Clients.All.SendAsync("BidDebug", payload);
    }

    // OnRuleSkipped is intentionally a no-op — would be too noisy in the UI
    public void OnRuleSkipped(string ruleName, DecisionContext context) { }

    public void PrintHands(Seat seat, Hand hand) { }

    public void OnBidDecisionComplete(RuleEvaluationLog log)
    {
        _ = _hubContext.Clients.All.SendAsync("BidDebugV2", log);
    }

    private static object BuildPayload(string ruleName, string bid, DecisionContext ctx)
    {
        return new
        {
            RuleName = ruleName,
            Bid = bid,
            Seat = ctx.Data.Seat.ToString(),
            Hand = ctx.Data.Hand.ToString(),
            Hcp = ctx.HandEvaluation.Hcp,
            Shape = ctx.HandEvaluation.Shape.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value),
            SeatRole = ctx.AuctionEvaluation.SeatRoleType.ToString(),
            AuctionPhase = ctx.AuctionEvaluation.AuctionPhase.ToString(),
            BiddingRound = ctx.AuctionEvaluation.BiddingRound,
            PartnerLastBid = ctx.AuctionEvaluation.PartnerLastBid?.ToString() ?? "—",
            TableKnowledge = ctx.TableKnowledge.Players.ToDictionary(
                kv => kv.Key.ToString(),
                kv => new
                {
                    kv.Value.HcpMin,
                    kv.Value.HcpMax,
                    MinShape = kv.Value.MinShape.ToDictionary(s => s.Key.ToString(), s => s.Value),
                    MaxShape = kv.Value.MaxShape.ToDictionary(s => s.Key.ToString(), s => s.Value),
                }),
        };
    }
}
