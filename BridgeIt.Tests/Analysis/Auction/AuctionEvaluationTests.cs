using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Auction;

[TestFixture]
public class AuctionEvaluationTests
{
    private BiddingDecision GetBiddingDecision(BidType bidType)
    {
        if (bidType == BidType.Pass)
        {
            return new BiddingDecision(Bid.Pass(), "", "");
        }

        if (bidType == BidType.NoTrumps)
        {
            return new BiddingDecision(Bid.NoTrumpsBid(1), "", "");
        }
        
        return new BiddingDecision(Bid.SuitBid(1, Suit.Clubs), "", "");
        
    }
    private static IEnumerable<TestCaseData> SeatRoleTestCases
    {
        get
        {
            yield return new TestCaseData(
                new[] { BidType.Pass, BidType.NoTrumps, BidType.Suit },
                Seat.North,
                Seat.East,
                SeatRole.Opener
            ).SetName("Pass then NT, East is Opener");
    
            yield return new TestCaseData(
                new[] { BidType.Pass, BidType.Pass },
                Seat.North,
                Seat.East,
                SeatRole.NoBids
            ).SetName("All Passes, No Bids");
    
            yield return new TestCaseData(
                new[] { BidType.Pass, BidType.Pass, BidType.NoTrumps },
                Seat.West,
                Seat.East,
                SeatRole.Opener
            ).SetName("NT then Pass, East is Responder");
            
            yield return new TestCaseData(
                new[] { BidType.Pass, BidType.Pass, BidType.NoTrumps },
                Seat.West,
                Seat.South,
                SeatRole.Overcaller
            ).SetName("East open, South is Overcaller");
    
            yield return new TestCaseData(
                new[] { BidType.NoTrumps, BidType.Suit },
                Seat.South,
                Seat.East,
                SeatRole.Overcaller
            ).SetName("NT then Suit, East is Overcaller");
        }
    }
    
    [Test]
    [TestCaseSource(nameof(SeatRoleTestCases))]
    public void GetSeatRole_ReturnsCorrectSeatRole(BidType[] bids, Seat dealer, Seat seatToTest, SeatRole expected)
    {
        var auctionBids = new List<AuctionBid>();
        foreach (var bid in bids)
        {
            auctionBids.Add(new AuctionBid(dealer, GetBiddingDecision(bid)));
        }

        var auctionHistory = new AuctionHistory(auctionBids, dealer);
    
        var result = AuctionEvaluator.GetSeatRole(auctionHistory, seatToTest);
    
        Assert.That(result, Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> BestFitSuitTestCases
    {
        get
        {
            yield return new TestCaseData(
                new Dictionary<string, int> { { "Hearts", 4 } },
                new Dictionary<Suit, int>
                {
                    { Suit.Spades, 8 },
                    { Suit.Hearts, 4 },
                    { Suit.Diamonds, 0 },
                    { Suit.Clubs, 0 }
                },
                Suit.Spades
            ).SetName("Spades longest suit");

            yield return new TestCaseData(
                new Dictionary<string, int> { { "Hearts", 4 } },
                new Dictionary<Suit, int>
                {
                    { Suit.Spades, 3 },
                    { Suit.Hearts, 4 },
                    { Suit.Diamonds, 8 },
                    { Suit.Clubs, 8 }
                },
                Suit.Hearts
            ).SetName("Hearts fit with partner");

            yield return new TestCaseData(
                new Dictionary<string, int> { { "Clubs", 5 } },
                new Dictionary<Suit, int>
                {
                    { Suit.Spades, 4 },
                    { Suit.Hearts, 4 },
                    { Suit.Diamonds, 0 },
                    { Suit.Clubs, 5 }
                },
                Suit.Clubs
            ).SetName("Clubs fit and longest");
        }
    }

    [Test]
    [TestCaseSource(nameof(BestFitSuitTestCases))]
    public void BestFitSuitTest(Dictionary<string, int> partnerShape, Dictionary<Suit, int> myShape, Suit expected)
    {
        var pk = new PartnershipKnowledge();
        foreach (var shape in partnerShape)
        {
            pk.PartnerMinShape[Enum.Parse<Suit>(shape.Key)] = shape.Value;
        }

        var result = pk.BestFitSuit(myShape);

        Assert.That(result, Is.EqualTo(expected));
    }
    
}