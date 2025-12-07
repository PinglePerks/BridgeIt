using BridgeIt.Core.BiddingEngine.Constraints;

namespace BridgeIt.Core.Domain.Bidding;

public sealed class BiddingDecision
{
    public Bid ChosenBid { get; }
    public string Explanation { get; }
    public string NextPartnershipState { get; }
    public bool AgreedPartnerSuit { get; init; }
    

    public BiddingDecision(Bid bid, string explanation, string nextState, bool agreedPartnerSuit = false)
    {
        ChosenBid = bid;
        Explanation = explanation;
        NextPartnershipState = nextState;
        AgreedPartnerSuit = agreedPartnerSuit;
    }
    
    public string PrettyPrint() => $"{ChosenBid} Expl: {Explanation} Next: {NextPartnershipState}";
}