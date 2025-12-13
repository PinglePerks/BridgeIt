using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dealer.Scenario;

public static class HandSpecification
{
    //puppet stayman
    public static Func<Hand, bool> BasicPuppetStaymanOpener =>
        h => HighCardPoints.Count(h) >= 20 && HighCardPoints.Count(h) <= 22 && ShapeEvaluator.IsBalanced(h);

    public static Func<Hand, bool> BasicPuppetStaymanResponder =>
        h => HighCardPoints.Count(h) >= 4 && ShapeEvaluator.GetShape(h)[Suit.Hearts] <= 4 && 
             ShapeEvaluator.GetShape(h)[Suit.Spades] <=4;
    
    public static Func<Dictionary<Seat,Hand>, bool> HasSpadeOrHeartFit(Seat opener, Seat responder) =>
        h => ShapeEvaluator.GetShape(h[opener])[Suit.Spades] + ShapeEvaluator.GetShape(h[responder])[Suit.Spades] >= 8
        || ShapeEvaluator.GetShape(h[opener])[Suit.Hearts] + ShapeEvaluator.GetShape(h[responder])[Suit.Hearts] >= 8;
    
    //
    
    
    
    
    
    
    
    


}