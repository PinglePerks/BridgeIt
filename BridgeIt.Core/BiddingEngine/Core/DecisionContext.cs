using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.Domain.IBidValidityChecker;

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
    public PartnershipKnowledge PartnershipKnowledge { get; init; }
    public AuctionEvaluation AuctionEvaluation { get; init; }

    public IBidValidityChecker ValidityChecker { get; }

    public DecisionContext(BiddingContext data, HandEvaluation handEvaluation, AuctionEvaluation auctionEvaluation, PartnershipKnowledge partnershipKnowledge)
    {
        Data = data;
        HandEvaluation = handEvaluation;
        AuctionEvaluation = auctionEvaluation;
        PartnershipKnowledge = partnershipKnowledge;
        ValidityChecker = new BidValidityChecker();
    }

    /// <summary>
    /// Determines whether the partnership should sign off, invite, or bid game
    /// based on combined HCP ranges.
    /// Major suit game = 25, NT game = 25, minor suit game = 29.
    /// </summary>
    public LevelVerdict GetLevelVerdict(int gameThreshold = 25)
    {
        var myHcp = HandEvaluation.Hcp;
        var combinedMin = PartnershipKnowledge.PartnerHcpMin + myHcp;
        var combinedMax = PartnershipKnowledge.PartnerHcpMax + myHcp;

        if (combinedMin >= gameThreshold) return LevelVerdict.BidGame;
        if (combinedMax < gameThreshold) return LevelVerdict.SignOff;
        return LevelVerdict.Invite;
    }
}