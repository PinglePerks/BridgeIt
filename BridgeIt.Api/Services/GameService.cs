using System.Collections.Concurrent;
using BridgeIt.Api.Models;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Api.Services;

public class GameService
{
    public ConcurrentDictionary<string, Seat> Players = new();
    
    private Dictionary<Seat, Hand> _currentDeal = new();

    public List<(Seat, Bid)> BidHistory = new();
    
    public Seat Dealer { get; private set; } = Seat.North; // Rotates every game
    public Seat NextBidder { get; private set; }
    public int ConsecutivePasses { get; private set; } = 0;
    public bool IsAuctionOver { get; private set; } = false;
    
    public Bid? HighestBid { get; private set; }
    
    public void ResetAuction()
    {
        NextBidder = Dealer; 
        BidHistory.Clear();
        ConsecutivePasses = 0;
        IsAuctionOver = false;
        HighestBid = null;
    }
    
    public bool IsValidBid(Bid newBid)
    {
        // 1. Logic for PASS
        if (newBid.Type == BidType.Pass) return true;

        // 2. Logic for DOUBLE (Must follow opponent bid)
        if (newBid.Type == BidType.Double) 
        {
            return HighestBid != null && !LastBidWasMyPartnership(NextBidder);
        }

        // 3. Logic for REDOUBLE (Must follow opponent Double)
        if (newBid.Type == BidType.Redouble)
        {
            var last = BidHistory.LastOrDefault();
            return last.Item2.Type == BidType.Double;
        }

        // 4. Standard Bid (Must be higher than current high bid)
        if (HighestBid == null) return true;

        return NewBidHigherThanLastBid(HighestBid, newBid); // You need a CompareTo method in your Shared.Bid
    }

    private bool NewBidHigherThanLastBid(Bid highestBid, Bid newBid)
    {
        if (newBid.Level > highestBid.Level) return true;
        if (newBid.Level == highestBid.Level)
        {
            if (newBid.Type == BidType.NoTrumps && highestBid.Type == BidType.Suit) return true;
            if (newBid.Suit > highestBid.Suit) return true;
        }
        return false;
    }
    
    public bool IsTurn(Seat seat) => seat == NextBidder;
    
    public void DealNewHand()
    {
        ResetAuction();
        var dealer = new Dealer.Deal.Dealer();
        _currentDeal = dealer.GenerateRandomDeal();
    }

    // STEP 2: Client says "Deal the cards!"
    public Hand  GetHandForPlayer(Seat seat)
    {
        return _currentDeal[seat];
    }
    
    public void DealCustom(CustomDealRequest req)
    {
        // Map the DTO to your Core Constraints
        // Using your Dealer's "GenerateConstrainedDeal" method
        ResetAuction();
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
        var dealer = new Dealer.Deal.Dealer();
        _currentDeal = dealer.GenerateConstrainedDeal(h => true, southConstraint);
        
    }
    private bool LastBidWasMyPartnership(Seat me)
    {
        if (BidHistory.Count == 0) return false;
        var lastBidder = BidHistory.Last().Item1;
        
        // North(0) and South(2) are partners (Even numbers)
        // East(1) and West(3) are partners (Odd numbers)
        return (int)me % 2 == (int)lastBidder % 2;
    }
    
    public void ProcessBid(Seat seat, Bid bid)
    {
        BidHistory.Add((seat, bid));

        if (bid.Type == BidType.Pass)
        {
            ConsecutivePasses++;
            if (ConsecutivePasses >= 3 && BidHistory.Count > 3) 
            {
                IsAuctionOver = true;
                // Logic to determine Contract goes here
            }
        }
        else
        {
            ConsecutivePasses = 0; // Reset pass count
            
            if (bid.Type != BidType.Redouble && bid.Type != BidType.Double)
            {
                HighestBid = bid;
            }
        }

        // Rotate Turn
        NextBidder = NextSeat(NextBidder);
    }

    private Seat NextSeat(Seat current)
    {
        return current switch
        {
            Seat.North => Seat.East,
            Seat.East => Seat.South,
            Seat.South => Seat.West,
            _ => Seat.North
        };
    }
}