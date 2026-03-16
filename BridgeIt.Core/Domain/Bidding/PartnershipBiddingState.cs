namespace BridgeIt.Core.Domain.Bidding;

enum PartnershipBiddingState {
    // 1. Initialization
    PRE_OPENING,          // No non-pass bids have been made yet.
    
    // 2. The Search Phase (No fit agreed yet)
    CONSTRUCTIVE_SEARCH,  // Our side opened. Exchanging shape/points. No interference.
    COMPETITIVE_SEARCH,   // Opponents have bid. We are searching for a fit under pressure.
    
    // 3. The Agreement Phase (Fit is known)
    FIT_ESTABLISHED,      // A trump suit or NT is agreed. Pivot point for level decisions.
    
    // 4. The Level Exploration Phase
    PART_SCORE_FIGHT,     // Both sides are bidding, competing for a low-level contract.
    GAME_INVITATIONAL,    // Asking partner: "If you have maximum points, bid Game."
    SLAM_EXPLORATION,     // Game is guaranteed. Exchanging Aces/Keycards (e.g., Blackwood).
    
    // 5. Termination & Resolution
    SIGN_OFF,             // A bid made expecting partner to pass (e.g., 4 Spades).
    PENALTY_EVALUATION,   // An opponent's contract is doubled. Deciding to sit or run.
    AUCTION_TERMINATED    // Three consecutive passes (or 4 passes at the start). Engine stops.
}