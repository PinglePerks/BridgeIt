using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;

namespace BridgeIt.Dealer.Deal;

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
    
    public List<Dictionary<Seat, Hand>> GenerateMultipleConstrainedDeals(long numberOfDeals,
        Func<Hand, bool> northConstraint,
        Func<Hand, bool>? eastConstraint = null,
        Func<Hand, bool>? southConstraint = null,
        Func<Hand, bool>? westConstraint = null)
    {
        // Simple "Monte Carlo" generation: Shuffle and check constraints.
        // For complex constraints, you might need a constructive builder.
        
        var deals = new List<Dictionary<Seat, Hand>>();

        for (var i = 0; i < numberOfDeals; i++)
        {
            deals.Add(GenerateConstrainedDeal(northConstraint, eastConstraint, southConstraint, westConstraint));
        }

        return deals;
    }
    
    public Dictionary<Seat, Hand> GenerateConstrainedDeal(
        Func<Hand, bool> northConstraint,
        Func<Hand, bool>? eastConstraint,
        Func<Hand, bool>? southConstraint,
        Func<Hand, bool>? westConstraint = null)
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
                    if (eastConstraint == null || eastConstraint(deal[Seat.East]))
                    {
                        if (westConstraint == null || westConstraint(deal[Seat.West]))

                            return deal;
                    }
                }
            }
            attempts++;
        }
        
        throw new Exception("Could not generate a hand meeting constraints after 100,000 attempts.");
    }
    
    public Dictionary<Seat, Hand> GenerateScenarioDeal(
        Func<Hand, bool> northConstraints,
        Func<Hand, bool> southConstraints,
        Func<Dictionary<Seat,Hand>, bool> boardConstraints)
    {
        // Simple "Monte Carlo" generation: Shuffle and check constraints.
        // For complex constraints, you might need a constructive builder.
        
        int attempts = 0;
        while (attempts < 100000)
        {
            var deal = GenerateRandomDeal();

            if (northConstraints(deal[Seat.North]) && southConstraints(deal[Seat.South]) && boardConstraints(deal)) return deal;
            attempts++;
        }



        throw new Exception("Could not generate a hand meeting constraints after 100,000 attempts.");
    }

    public Dictionary<Seat, Hand> GeneratePuppetDeal(Seat opener = Seat.North, Seat responder = Seat.South)
        => GenerateScenarioDeal(HandSpecification.BasicPuppetStaymanOpener,
            HandSpecification.BasicPuppetStaymanResponder,
            HandSpecification.HasSpadeOrHeartFit(opener, responder));


}