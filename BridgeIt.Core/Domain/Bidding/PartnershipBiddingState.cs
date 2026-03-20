namespace BridgeIt.Core.Domain.Bidding;

public enum PartnershipBiddingState
{
    Unknown,             // Default — no state established yet
    ConstructiveSearch,  // Opened, exchanging shape/strength, no fit agreed
    FitEstablished,      // A suit (or NT) has been agreed — pivot point
    GameInvitational,    // Inviting game; partner decides
    SlamExploration,     // Game is safe; probing for slam (Blackwood, cue bids)
    SignOff,             // Expecting pass
}