using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol._2NT_opening;

/// <summary>
/// System tests for conventions after a 2NT opening (20-22 balanced).
/// Tests Stayman at the 3-level and red-suit transfers at the 3-level.
/// </summary>
[TestFixture]
public class Acol2NTConventionTests
{
    private TestBridgeEnvironment _environment;
    private Dealer.Deal.Dealer _dealer;

    [OneTimeSetUp]
    public void Setup()
    {
        _environment = TestBridgeEnvironment.Create().WithAllRules();
        _dealer = new Dealer.Deal.Dealer();
    }

    // =============================================
    // 2NT Opening
    // =============================================

    [Test]
    public async Task Opener_AlwaysBids2NT_WithBalanced20to22()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol2NtOpening,
            HandSpecification.PassingOpponent);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids.First().Bid.ToString();

            Assert.That(openingBid, Is.EqualTo("2NT"),
                $"Failed with hand: {deal[Seat.North]}");
        }
    }

    // =============================================
    // Transfers over 2NT
    // =============================================

    [Test]
    public async Task ResponseTo2NT_TransfersToHearts_With5Hearts()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol2NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo2NT_TransferHearts);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids[0].Bid.ToString();
            var responseBid = auction.Bids[2].Bid.ToString();
            var completionBid = auction.Bids[4].Bid.ToString();

            Assert.That(openingBid, Is.EqualTo("2NT"),
                $"Opening failed with hand: {deal[Seat.North]}");
            Assert.That(responseBid, Is.EqualTo("3D"),
                $"Transfer failed with hand: {deal[Seat.South]}");
            Assert.That(completionBid, Is.EqualTo("3H"),
                $"Completion failed with hand: {deal[Seat.North]}");
        }
    }

    [Test]
    public async Task ResponseTo2NT_TransfersToSpades_With5Spades()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol2NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo2NT_TransferSpades);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids[0].Bid.ToString();
            var responseBid = auction.Bids[2].Bid.ToString();
            var completionBid = auction.Bids[4].Bid.ToString();

            Assert.That(openingBid, Is.EqualTo("2NT"),
                $"Opening failed with hand: {deal[Seat.North]}");
            Assert.That(responseBid, Is.EqualTo("3H"),
                $"Transfer failed with hand: {deal[Seat.South]}");
            Assert.That(completionBid, Is.EqualTo("3S"),
                $"Completion failed with hand: {deal[Seat.North]}");
        }
    }

    // =============================================
    // Stayman over 2NT
    // =============================================

    [Test]
    public async Task ResponseTo2NT_Stayman_With4CardMajor()
    {
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol2NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo2NT_Stayman);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);
            var openingBid = auction.Bids[0].Bid.ToString();
            var responseBid = auction.Bids[2].Bid.ToString();

            Assert.That(openingBid, Is.EqualTo("2NT"),
                $"Opening failed with hand: {deal[Seat.North]}");
            Assert.That(responseBid, Is.EqualTo("3C"),
                $"Stayman failed with hand: {deal[Seat.South]}");

            // Opener's Stayman response should be 3D, 3H, or 3S
            var staymanResponse = auction.Bids[4].Bid.ToString();
            Assert.That(staymanResponse, Is.AnyOf("3D", "3H", "3S"),
                $"Stayman response '{staymanResponse}' invalid. Opener: {deal[Seat.North]}");
        }
    }
}
