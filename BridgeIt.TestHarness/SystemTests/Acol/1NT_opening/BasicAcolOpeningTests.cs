using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol._1NT_opening;

[TestFixture]
public class BasicAcolOpeningTests
{
    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;

    [OneTimeSetUp]
    public void Setup()
    {
        // Load the specific Acol JSON manifest or YAML folder
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer(); // Your hand generator
    }
    
    [Test]
    public async Task ResponseTo1NT_AlwaysTransfersToHearts_With5Hearts()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced
        
        var minShape = new Dictionary<Suit, int> { {Suit.Hearts, 5} };
        var maxShape = new Dictionary<Suit, int> { {Suit.Spades, 4} };

        var responder = HandSpecification.Generator(0, 20, minShape, maxShape);
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            var openerReBid = auction.Bids[4].Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("2D"), $"Failed with hand: {deal[Seat.South]}");
            Assert.That(openerReBid, Is.EqualTo("2H"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
    [Test]
    public async Task ResponseTo1NT_AlwaysTransfersToSpades_With5Spades()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced
        
        var minShape = new Dictionary<Suit, int> { {Suit.Spades, 5} };
        var maxShape = new Dictionary<Suit, int> { {Suit.Hearts, 4} };
        
        var responder = HandSpecification.Generator(0, 20, minShape, maxShape );
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            var openerReBid = auction.Bids[4].Bid.ToString();

            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("2H"), $"Failed with hand: {deal[Seat.South]}");
            Assert.That(openerReBid, Is.EqualTo("2S"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
    [Test]
    public async Task ResponseTo1NT_Stayman_WithPointsAndHearts()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced
        
        var minShape = new Dictionary<Suit, int> { {Suit.Hearts, 4} };
        var maxShape = new Dictionary<Suit, int> { {Suit.Hearts, 4}, {Suit.Spades, 4} };

        var responder = HandSpecification.Generator(11, 20, minShape, maxShape);
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("2C"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
    [Test]
    public async Task ResponseTo1NT_Stayman_WithPointsAndSpades()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced
        
        var minShape = new Dictionary<Suit, int> { {Suit.Spades, 4} };
        var maxShape = new Dictionary<Suit, int> { {Suit.Spades, 4}, {Suit.Hearts, 4} };

        var responder = HandSpecification.Generator(11, 20, minShape, maxShape);
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("2C"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
    [Test]
    public async Task ResponseTo1NT_Bid3NT_WithPointsAndNoMajor()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced

        var minShape = new Dictionary<Suit, int>();
        var maxShape = new Dictionary<Suit, int> { {Suit.Spades, 3}, {Suit.Hearts, 3} };

        var responder = HandSpecification.Generator(13, 18, minShape, maxShape);
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("3NT"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
    [Test]
    public async Task ResponseTo1NT_Bid2NT_WithPointsAndNoMajor()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced

        var minShape = new Dictionary<Suit, int>();
        var maxShape = new Dictionary<Suit, int> { {Suit.Spades, 3}, {Suit.Hearts, 3} };

        var responder = HandSpecification.Generator(12, 12, minShape, maxShape);
            
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass, responder);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            var responderBid = auction.Bids[2].Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
            Assert.That(responderBid, Is.EqualTo("2NT"), $"Failed with hand: {deal[Seat.South]}");
        }
    }
    
}