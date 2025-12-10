

using System.ComponentModel.DataAnnotations;
using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Players;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine : IPlayer
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;

    public BidInformation GetConstraintsFromBid(DecisionContext decisionContext, Bid bid)
    {
        foreach (var rule in _rules)
        {
            var bidInformation = rule.GetConstraintForBid(bid, decisionContext);
             if (bidInformation != null)
                 return bidInformation;
        }

        return new BidInformation(bid, null, null);
    }
    
    public BiddingContext CreateBiddingContext(Seat currentSeat, Hand currentHand, AuctionHistory auctionHistory)
    {
        return new BiddingContext(
            hand: currentHand,
            auctionHistory: auctionHistory,
            seat: currentSeat,
            vulnerability: Vulnerability.None);

    } 
    
    protected internal List<BidInformation> GetPartnerConstraints(BiddingContext ctx, IRuleLookupService ruleLookupService)
    {
        var dict = ruleLookupService.GetConstraintsFromBids(ctx, this);
        
        var partner = ctx.Seat.GetPartner();
        
        return dict[partner];

    }

    public BiddingEngine(IEnumerable<IBiddingRule> rules, ILogger<BiddingEngine> logger)
    {
        _rules = rules.OrderByDescending(r => r.Priority).ToList();
        _logger = logger;
    }

    public Bid ChooseBid(BiddingContext ctx)
    {
        var decisionContext = new DecisionContext(ctx);
        return ChooseBid(decisionContext);
    }

    public Bid ChooseBid(DecisionContext ctx)
    {
        
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
                _logger.LogDebug($"\n Seat: {ctx.Data.Seat}\nRule {rule.Name} applied\n Bid:{decision}\n");
                return decision;
            }
        }
        // fallback
        return Bid.Pass();
    }

    
    public Task<Bid> GetBidAsync(BiddingContext context)
    {
        throw new NotImplementedException();
    }
}