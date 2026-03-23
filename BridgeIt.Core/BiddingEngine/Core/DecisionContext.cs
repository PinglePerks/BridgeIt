using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.BiddingEngine.Core;

public enum LevelVerdict
{
    SignOff,   // Combined maximum can't reach game threshold
    Invite,    // Combined range straddles game threshold — partner decides
    BidGame,   // Combined minimum already reaches game threshold
}

public class DecisionContext
{
    public BiddingContext Data { get; init; }
    public HandEvaluation HandEvaluation { get; init; }
    public TableKnowledge TableKnowledge { get; init; }
    public AuctionEvaluation AuctionEvaluation { get; init; }

    public PartnershipBiddingState PartnershipBiddingState { get; init; }

    public DecisionContext(BiddingContext data, HandEvaluation handEvaluation, AuctionEvaluation auctionEvaluation, TableKnowledge tableKnowledge, PartnershipBiddingState partnershipBiddingState = PartnershipBiddingState.Unknown)
    {
        Data = data;
        HandEvaluation = handEvaluation;
        AuctionEvaluation = auctionEvaluation;
        TableKnowledge = tableKnowledge;
        PartnershipBiddingState = partnershipBiddingState;
    }

    // --- Combined partnership queries (my hand + partner's inferred range) ---

    public int CombinedHcpMin => HandEvaluation.Hcp + TableKnowledge.Partner.HcpMin;
    public int CombinedHcpMax => HandEvaluation.Hcp + TableKnowledge.Partner.HcpMax;

    /// <summary>
    /// Determines whether the partnership should sign off, invite, or bid game
    /// based on combined HCP ranges.
    /// Major suit game = 25, NT game = 25, minor suit game = 29.
    /// </summary>
    public LevelVerdict GetLevelVerdict(int gameThreshold = 25)
    {
        if (CombinedHcpMin >= gameThreshold) return LevelVerdict.BidGame;
        if (CombinedHcpMax < gameThreshold) return LevelVerdict.SignOff;
        return LevelVerdict.Invite;
    }

    /// <summary>
    /// Do we definitely have an 8+ card fit in this suit (my hand + partner's minimum)?
    /// </summary>
    public bool HasFitInSuit(Suit suit, int requiredCombined = 8)
    {
        var myLength = HandEvaluation.Shape[suit];
        var partnerMin = TableKnowledge.Partner.MinShape[suit];
        return myLength + partnerMin >= requiredCombined;
    }

    /// <summary>
    /// Could we possibly have an 8+ card fit in this suit (my hand + partner's maximum)?
    /// </summary>
    public bool HasPossibleFitInSuit(Suit suit, int requiredCombined = 8)
    {
        var myLength = HandEvaluation.Shape[suit];
        var partnerMax = TableKnowledge.Partner.MaxShape[suit];
        return myLength + partnerMax >= requiredCombined;
    }

    /// <summary>
    /// Find the best confirmed fit suit (8+ combined), or null if none.
    /// </summary>
    public Suit? BestFitSuit()
    {
        foreach (var suit in HandEvaluation.Shape.Keys)
        {
            if (HasFitInSuit(suit)) return suit;
        }
        return null;
    }
}
