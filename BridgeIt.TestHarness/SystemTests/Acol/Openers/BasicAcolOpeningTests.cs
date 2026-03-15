using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol.Openers;

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
    public async Task Opener_AlwaysBids1NT_WithBalanced12to14()
    {
        // Generate 50 hands that are strictly 12-14 points and balanced
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol1NtOpening, HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1NT"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids2NT_WithBalanced20to22()
    {
        // Generate 50 hands that are strictly 20-22 points and balanced
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.Acol2NtOpening, HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("2NT"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids1S_WithLongSpadesAndOpeningStrength()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolMajor1LevelOpening(Suit.Spades), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1S"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids1H_WithLongHeartsAndOpeningStrength()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolMajor1LevelOpening(Suit.Hearts), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1H"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids1D_WithLongDiamondsAndOpeningStrength()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolMinor1LevelOpening(Suit.Diamonds), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1D"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysBids1C_WithLongDiamondsAndOpeningStrength()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolMinor1LevelOpening(Suit.Clubs), HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("1C"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
    [Test]
    public async Task Opener_AlwaysPass_WithWeakHandAndNoLength()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(50, HandSpecification.AcolOpeningPass);

        foreach(var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();
            
            Assert.That(openingBid, Is.EqualTo("Pass"), $"Failed with hand: {deal[Seat.North]}");
        }
    }
    
}