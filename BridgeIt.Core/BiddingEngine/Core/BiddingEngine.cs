

using BridgeIt.Core.Domain.Bidding;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;

    public BiddingEngine(IEnumerable<IBiddingRule> rules, ILogger<BiddingEngine> logger)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
        _logger = logger;
    }

    public BiddingDecision ChooseBid(BiddingContext ctx)
    {
        foreach (var rule in _rules)
        {

            if (!rule.IsApplicable(ctx))
            {
                _logger.LogInformation($"Rule {rule.GetType().Name} is not applicable");
                continue;
            }
            _logger.LogInformation($"Rule {rule.GetType().Name} is applicable");

            var decision = rule.Apply(ctx);


            if (decision != null)
            {
                _logger.LogInformation($"Rule {rule.GetType().Name} applied");
                return decision;
            }

        }

        // fallback
        return new BiddingDecision(Bid.Pass(), "No applicable rule found", "passed");
    }
}