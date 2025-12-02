using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.Deal;
using BridgeIt.WebDeal.Models;

namespace BridgeIt.WebDeal.Services;

public class DealService
{
    private readonly IDealer _dealer; // If you inject it, or just new() it if simple

    public DealService(IDealer dealer) // Or constructorless if simple
    {
        _dealer = dealer;
    } 
    public Dictionary<Seat, Hand> DealRandomHand()
    {
        var deck = new Deck();
        deck.Shuffle();

        return new Dictionary<Seat, Hand>
        {
            { Seat.North, new Hand(deck.Cards.Take(13)) },
            { Seat.East, new Hand(deck.Cards.Skip(13).Take(13)) },
            { Seat.South, new Hand(deck.Cards.Skip(26).Take(13)) },
            { Seat.West, new Hand(deck.Cards.Skip(39).Take(13)) }
        };
    }
    
    public Dictionary<Seat, Hand> DealCustom(CustomDealRequest req)
    {
        // Map the DTO to your Core Constraints
        // Using your Dealer's "GenerateConstrainedDeal" method
        
        Func<Hand, bool> southConstraint = h =>
        {
            // 1. HCP Check or Losers Check
            if (req.HcpCheck == "hcp")
            {
                int pts = HighCardPoints.Count(h);
                if (pts < req.MinHcp || pts > req.MaxHcp) return false;
            }
            else
            {
                int losers = LosingTrickCount.Count(h);
                if (losers < req.MinLosers || losers > req.MaxLosers) return false;
            }

            // 2. Balanced Check
            if (req.BalancedCheck == "balanced" && !ShapeEvaluator.IsBalanced(h)) return false;

            // 3. Suit Check
            if (req.BalancedCheck != "balanced")
            {
                if (ShapeEvaluator.IsBalanced(h)) return false;
                
                var shape = ShapeEvaluator.GetShape(h);
                foreach (var kvp in req.Shape)
                {
                    if (kvp.Value == null) continue;
                    if (shape[kvp.Key] != kvp.Value) return false;
                }
            }

            return true;
        };
        
        if (_dealer is Dealer.Deal.Dealer dealer)
        {
            return dealer.GenerateConstrainedDeal(h => true, southConstraint);
        }
        
        throw new InvalidOperationException("Dealer does not support constraints.");
    }
    
    
}
