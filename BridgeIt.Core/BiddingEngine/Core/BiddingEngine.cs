

using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.BiddingEngine.Constraints;
using BridgeIt.Core.BiddingEngine.RuleLookupService;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Core.Gameplay.Services;
using Microsoft.Extensions.Logging;

namespace BridgeIt.Core.BiddingEngine.Core;

public sealed class BiddingEngine
{
    private readonly List<IBiddingRule> _rules;
    private readonly ILogger<BiddingEngine> _logger;

    public IBidConstraint? GetConstraintsFromBid(BiddingContext biddingContext, Bid bid)
    {
        foreach (var rule in _rules)
        {
            var constraint = rule.GetConstraintForBid(bid, biddingContext);
             if (constraint != null)
                 return constraint;
        }

        return null;
    }
    
    public BiddingContext CreateBiddingContext(Seat currentSeat, Hand currentHand, AuctionHistory auctionHistory, Seat dealer, ISeatRotationService rotation, IRuleLookupService ruleLookupService)
    {
        return new BiddingContext(
            hand: currentHand,   
            auctionHistory: auctionHistory,
            seat: currentSeat,
            vulnerability: Vulnerability.None,
            handEvaluation: HandEvaluator.Evaluate(currentHand),
            partnershipKnowledge: AuctionEvaluator.AnalyzeKnowledge(GetPartnerConstraints(auctionHistory.Bids, Vulnerability.None, dealer, rotation.PartnerOf(currentSeat),ruleLookupService)),
            auctionEvaluation: AuctionEvaluator.Evaluate(auctionHistory, currentSeat)
        );
        
    } 
    
    protected internal List<IBidConstraint> GetPartnerConstraints(IReadOnlyList<AuctionBid> bidHistory, Vulnerability vulnerability, Seat dealer, Seat partner, IRuleLookupService ruleLookupService)
    {
        var dict = ruleLookupService.GetConstraintsFromBids(bidHistory, vulnerability, dealer, this);
        
        return dict[partner];

    }

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