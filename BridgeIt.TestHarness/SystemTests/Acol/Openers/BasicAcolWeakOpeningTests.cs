using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol.Openers;

public class BasicAcolWeakOpeningTests
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
    public async Task Opener_AlwaysBids2H_WithWeakAndLongHearts()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolWeakAndLongOpening(Suit.Hearts, 6), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("2H"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids2S_WithWeakAndLongSpades()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolWeakAndLongOpening(Suit.Spades), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("2S"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids2D_WithWeakAndLongDiamonds()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolWeakAndLongOpening(Suit.Diamonds), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("2D"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysPasses_WithWeakAnd6Clubs()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolWeakAndLongOpening(Suit.Clubs), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("Pass"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids3D_WithWeakAnd7LongDiamonds()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolWeakAndLongOpening(Suit.Diamonds, 7));

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("3D"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    
}