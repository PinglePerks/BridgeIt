

using BridgeIt.Core.Domain.Bidding;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;

    public BiddingEngine(IEnumerable<IBiddingRule> rules)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
    }

    public BiddingDecision ChooseBid(BiddingContext ctx)
    {
        foreach (var rule in _rules)
        {
            
            if (!rule.IsApplicable(ctx))
                continue;

            var decision = rule.Apply(ctx);

            if (decision != null)
                return decision;
        }

        // fallback
        return new BiddingDecision(Bid.Pass(), "No applicable rule found", "passed");
    }
}