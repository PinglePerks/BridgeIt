using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dealer.Deal;

public interface IDealer
{
    Dictionary<Seat, Hand> GenerateRandomDeal();
}

public class Dealer : IDealer
{
    public Dictionary<Seat, Hand> GenerateRandomDeal()
    {
        var deck = new Deck();
        deck.Shuffle();
        
        return new Dictionary<Seat, Hand>
        {
            { Seat.North, new Hand(deck.Cards.Take(13)) },
            { Seat.East,  new Hand(deck.Cards.Skip(13).Take(13)) },
            { Seat.South, new Hand(deck.Cards.Skip(26).Take(13)) },
            { Seat.West,  new Hand(deck.Cards.Skip(39).Take(13)) }
        };
    }

    public Dictionary<Seat, Hand> GenerateConstrainedDeal(
        Func<Hand, bool> northConstraint,
        Func<Hand, bool>? southConstraint)
    {
        // Simple "Monte Carlo" generation: Shuffle and check constraints.
        // For complex constraints, you might need a constructive builder.
        
        int attempts = 0;
        while(attempts < 100000)
        {
            var deal = GenerateRandomDeal();
            
            if (northConstraint(deal[Seat.North]))
            {
                // If we also care about South (e.g. finding a fit)
                if (southConstraint == null || southConstraint(deal[Seat.South]))
                {
                    return deal;
                }
            }
            attempts++;
        }
        
        throw new Exception("Could not generate a hand meeting constraints after 100,000 attempts.");
    }
    
}

