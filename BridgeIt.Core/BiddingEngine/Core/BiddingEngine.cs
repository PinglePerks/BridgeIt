

using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
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
        var tmpPartner = ctx.PartnershipKnowledge;

        _logger.LogInformation($"Partner - min hcp: {tmpPartner.PartnerHcpMin}\n" +
                               $"max hcp: {tmpPartner.PartnerHcpMax}\n" +
                               $"shape min clubs: {tmpPartner.PartnerMinShape[Suit.Clubs]}\n" +
                               $"shape min diamonds: {tmpPartner.PartnerMinShape[Suit.Diamonds]}\n" +
                               $"shape min hearts: {tmpPartner.PartnerMinShape[Suit.Hearts]}\n" +
                               $"shape min spades: {tmpPartner.PartnerMinShape[Suit.Spades]}\n");
        foreach (var rule in _rules)
        {
            if (!rule.IsApplicable(ctx))
            {
                continue;
            }
            _logger.LogDebug($"Rule {rule.Name} is applicable");
            
            var decision = rule.Apply(ctx);
            
            if (decision != null)
            {
                _logger.LogDebug($"\n Seat: {ctx.Seat}\nRule {rule.Name} applied\n Bid:{decision.ChosenBid}\n");
                return decision;
            }
        }
        // fallback
        return new BiddingDecision(Bid.Pass(), "No applicable rule found", "passed");
    }
}