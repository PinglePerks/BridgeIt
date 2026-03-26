using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;


namespace BridgeIt.Core.Analysis.Auction;

public class AuctionEvaluation
{
    public SeatRoleType SeatRoleType { get; init; }
    public Bid? CurrentContract { get; init; }
    public Bid? OpeningBid { get; init; }
    public Bid? PartnerLastBid { get; init; }

    /// <summary>Last non-pass bid made by partner (skips passes)</summary>
    public Bid? PartnerLastNonPassBid { get; init; }

    /// <summary>Last non-pass bid made by the current seat</summary>
    public Bid? MyLastNonPassBid { get; init; }

    public Seat? OpeningSeat {get; init;}
    public Seat? NextSeatToBid { get; init; }
    public AuctionPhase AuctionPhase { get; init; }
    public int BiddingRound { get; init; }

    // ── Competitive bidding properties ──────────────────────────────

    /// <summary>Last non-pass bid made by right-hand opponent.</summary>
    public Bid? RhoLastNonPassBid { get; init; }

    /// <summary>Last non-pass bid made by left-hand opponent.</summary>
    public Bid? LhoLastNonPassBid { get; init; }

    /// <summary>Distinct suits bid by the opposing partnership.</summary>
    public IReadOnlyList<Suit> OpponentBidSuits { get; init; } = Array.Empty<Suit>();

    /// <summary>Suits not yet bid by anyone in the auction.</summary>
    public IReadOnlyList<Suit> UnbidSuits { get; init; } = Array.Empty<Suit>();

    /// <summary>True when acting immediately after an opponent's bid (direct seat).</summary>
    public bool IsDirectSeat { get; init; }

    /// <summary>True when acting in protective/balancing position (opponent bid, two passes, your turn).</summary>
    public bool IsProtectiveSeat { get; init; }
}

public static class AuctionEvaluator
{
    public static AuctionEvaluation Evaluate(AuctionHistory auctionHistory)
    {
        var currentSeat = GetNextSeatToBid(auctionHistory);

        return new AuctionEvaluation()
        {
            NextSeatToBid = currentSeat,
            CurrentContract = GetCurrentContract(auctionHistory),
            SeatRoleType = GetSeatRole(auctionHistory),
            OpeningBid = GetOpeningBid(auctionHistory),
            PartnerLastBid = GetPartnerLastBid(auctionHistory),
            PartnerLastNonPassBid = GetPartnerLastNonPassBid(auctionHistory),
            MyLastNonPassBid = GetMyLastNonPassBid(auctionHistory),
            OpeningSeat = GetOpeningSeat(auctionHistory),
            AuctionPhase = GetAuctionPhase(auctionHistory),
            BiddingRound = GetBiddingRound(auctionHistory),
            RhoLastNonPassBid = GetRhoLastNonPassBid(auctionHistory, currentSeat),
            LhoLastNonPassBid = GetLhoLastNonPassBid(auctionHistory, currentSeat),
            OpponentBidSuits = GetOpponentBidSuits(auctionHistory, currentSeat),
            UnbidSuits = GetUnbidSuits(auctionHistory),
            IsDirectSeat = GetIsDirectSeat(auctionHistory, currentSeat),
            IsProtectiveSeat = GetIsProtectiveSeat(auctionHistory, currentSeat),
        };
    }
    
    private static int GetBiddingRound(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
    
        var openingBidIndex = history.Bids
            .Select((b, i) => (b, i))
            .FirstOrDefault(x => x.b.Bid.Type != BidType.Pass).i;
    
        // PreOpening — no one has opened yet
        if (history.Bids.All(b => b.Bid.Type == BidType.Pass)) return 0;

        // Count every turn (pass or bid) the current seat has had since the opening bid
        var turnsSinceOpening = history.Bids
            .Skip(openingBidIndex)
            .Count(b => b.Seat == currentSeat);

        return turnsSinceOpening + 1;
    }

    
    private static Bid? GetPartnerLastBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        var partnerSeat = currentSeat.GetPartner();
        return history.Bids.LastOrDefault(b => b.Seat == partnerSeat)?.Bid;
    }

    private static Bid? GetPartnerLastNonPassBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        var partnerSeat = currentSeat.GetPartner();
        return history.Bids
            .LastOrDefault(b => b.Seat == partnerSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }

    private static Bid? GetMyLastNonPassBid(AuctionHistory history)
    {
        var currentSeat = GetNextSeatToBid(history);
        return history.Bids
            .LastOrDefault(b => b.Seat == currentSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }
    
    private static AuctionPhase GetAuctionPhase(AuctionHistory history)
    {
        var nonPassBids = history.Bids.Where(b => b.Bid.Type != BidType.Pass).ToList();
        if (!nonPassBids.Any()) return AuctionPhase.PreOpening;

        var openingSeat = nonPassBids.First().Seat;
        var oppositionHasBid = nonPassBids.Any(b => b.Seat != openingSeat && b.Seat != openingSeat.GetPartner());
    
        return oppositionHasBid ? AuctionPhase.Contested : AuctionPhase.Uncontested;
    }


    
    private static Seat GetNextSeatToBid(AuctionHistory auctionHistory)
    {
        var auctionBid = auctionHistory.Bids.LastOrDefault();
        
        return auctionBid == null ? auctionHistory.Dealer : auctionBid.Seat.GetNextSeat();
    }
    
    
    private static Bid? GetCurrentContract(AuctionHistory auctionHistory)
        => auctionHistory.Bids
            .LastOrDefault(x => x.Bid.Type == BidType.NoTrumps || x.Bid.Type == BidType.Suit)?
            .Bid;
    
    private static Seat? GetOpeningSeat(AuctionHistory auctionHistory) 
        => auctionHistory.Bids.FirstOrDefault(x => x.Bid.Type != BidType.Pass)?.Seat;
    
    private static Bid? GetOpeningBid(AuctionHistory auctionHistory) 
        => auctionHistory.Bids.FirstOrDefault(x => x.Bid.Type != BidType.Pass)?.Bid;


    public static SeatRoleType GetSeatRole(AuctionHistory auctionHistory)
    {
        var currentSeat = GetNextSeatToBid(auctionHistory);

        var openingSeat = GetOpeningSeat(auctionHistory);
        if (openingSeat == null) return SeatRoleType.NoBids;

        if (openingSeat == currentSeat) return SeatRoleType.Opener;

        var difference = ((int)currentSeat - (int)openingSeat + 4) % 4;

        return difference switch
        {
            1 => SeatRoleType.Overcaller,
            2 => SeatRoleType.Responder,
            3 => SeatRoleType.Overcaller,
            _ => throw new ArgumentOutOfRangeException()
        };

    }

    // ── Competitive bidding helpers ──────────────────────────────────

    private static Bid? GetRhoLastNonPassBid(AuctionHistory history, Seat currentSeat)
    {
        // RHO is the seat immediately before currentSeat
        var rho = ((int)currentSeat - 1 + 4) % 4;
        var rhoSeat = (Seat)rho;
        return history.Bids
            .LastOrDefault(b => b.Seat == rhoSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }

    private static Bid? GetLhoLastNonPassBid(AuctionHistory history, Seat currentSeat)
    {
        var lhoSeat = currentSeat.GetNextSeat();
        return history.Bids
            .LastOrDefault(b => b.Seat == lhoSeat && b.Bid.Type != BidType.Pass)?.Bid;
    }

    private static IReadOnlyList<Suit> GetOpponentBidSuits(AuctionHistory history, Seat currentSeat)
    {
        var partnerSeat = currentSeat.GetPartner();
        return history.Bids
            .Where(b => b.Seat != currentSeat && b.Seat != partnerSeat
                        && b.Bid.Type == BidType.Suit && b.Bid.Suit.HasValue)
            .Select(b => b.Bid.Suit!.Value)
            .Distinct()
            .ToList();
    }

    private static IReadOnlyList<Suit> GetUnbidSuits(AuctionHistory history)
    {
        var bidSuits = history.Bids
            .Where(b => b.Bid.Type == BidType.Suit && b.Bid.Suit.HasValue)
            .Select(b => b.Bid.Suit!.Value)
            .ToHashSet();

        return Enum.GetValues<Suit>().Where(s => !bidSuits.Contains(s)).ToList();
    }

    /// <summary>
    /// Direct seat: the last bid in the auction (immediately before this seat)
    /// was a non-pass bid by an opponent. i.e. RHO just bid.
    /// </summary>
    private static bool GetIsDirectSeat(AuctionHistory history, Seat currentSeat)
    {
        if (!history.Bids.Any()) return false;
        var lastBid = history.Bids.Last();
        var partnerSeat = currentSeat.GetPartner();
        // Last bid was by RHO and was not a pass
        return lastBid.Seat != currentSeat
               && lastBid.Seat != partnerSeat
               && lastBid.Bid.Type != BidType.Pass;
    }

    /// <summary>
    /// Protective seat: opponent bid, then partner passed, then RHO passed, now it's our turn.
    /// Pattern: opponent bid ... partner Pass, RHO Pass → current seat.
    /// </summary>
    private static bool GetIsProtectiveSeat(AuctionHistory history, Seat currentSeat)
    {
        if (history.Bids.Count < 3) return false;

        var bids = history.Bids;
        var last = bids[^1]; // RHO's bid (should be pass)
        var secondLast = bids[^2]; // partner's bid (should be pass)

        var partnerSeat = currentSeat.GetPartner();

        // RHO passed, partner passed
        if (last.Bid.Type != BidType.Pass) return false;
        if (secondLast.Bid.Type != BidType.Pass || secondLast.Seat != partnerSeat) return false;

        // The bid before that should be a non-pass by an opponent (LHO)
        if (bids.Count < 3) return false;
        var thirdLast = bids[^3];
        return thirdLast.Seat != currentSeat
               && thirdLast.Seat != partnerSeat
               && thirdLast.Bid.Type != BidType.Pass;
    }
}

public enum AuctionPhase
{
    PreOpening,    // No non-pass bids have been made
    Uncontested,   // One side has opened; opponents have only passed
    Contested,     // Both sides have made non-pass bids
    //Terminated    // Auction closed (3+ consecutive passes after a contract exists)
}