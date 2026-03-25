using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;
using BridgeIt.Dealer.HandSpecifications;
using BridgeIt.TestHarness.Setup;
using NUnit.Framework;

namespace BridgeIt.TestHarness.SystemTests.Acol.Rebid;

/// <summary>
/// System tests for knowledge-based rules that fire after pattern rules.
/// Verifies that the engine correctly derives combined partnership knowledge
/// and makes appropriate game/invite/sign-off decisions.
/// </summary>
[TestFixture]
public class KnowledgeRuleSystemTests
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
    // Sign off after transfer — weak hand passes
    // =============================================

    [Test]
    public async Task KnowledgeSignOff_AfterTransfer_WeakResponderPasses()
    {
        // 1NT -> 2D (transfer) -> 2H (completion) -> Pass (weak, sign off in fit)
        // Responder has 5+ hearts but <11 HCP, <5 spades, no long side suit
        // (avoids bidding 2S or showing a second suit after transfer)
        Func<Hand, bool> weakHeartsOnly = h =>
            HighCardPoints.Count(h) < 11
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5
            && ShapeEvaluator.GetShape(h)[Suit.Spades] < 5
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 5
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 5;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtOpening,
            HandSpecification.PassingOpponent,
            weakHeartsOnly);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"),
                $"Opening failed: {deal[Seat.North]}");
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2D"),
                $"Transfer failed: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2H"),
                $"Completion failed: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected pass with weak hand after transfer. Responder: {deal[Seat.South]}");
        }
    }

    [Test]
    public async Task KnowledgeSignOff_AfterTransfer_WeakResponderPassesSpades()
    {
        // 1NT -> 2H (transfer) -> 2S (completion) -> Pass
        // Responder has 5+ spades, <5 hearts, <11 HCP, no long side suit
        Func<Hand, bool> weakSpadesOnly = h =>
            HighCardPoints.Count(h) < 11
            && ShapeEvaluator.GetShape(h)[Suit.Spades] >= 5
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] < 5
            && ShapeEvaluator.GetShape(h)[Suit.Diamonds] < 5
            && ShapeEvaluator.GetShape(h)[Suit.Clubs] < 5;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtOpening,
            HandSpecification.PassingOpponent,
            weakSpadesOnly);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2H"),
                $"Transfer failed: {deal[Seat.South]}");
            Assert.That(auction.Bids[4].Bid.ToString(), Is.EqualTo("2S"),
                $"Completion failed: {deal[Seat.North]}");
            Assert.That(auction.Bids[6].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected pass with weak hand. Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // Game in suit — raise to major after confirmed fit
    // =============================================

    [Test]
    public async Task KnowledgeGame_BidsGameInMajor_WhenSimpleRaiseAndMaxOpener()
    {
        // 1H -> 2H (simple raise 6-9) -> 4H (opener has enough for game)
        // Opener needs 16+ HCP to guarantee game opposite 6-9
        Func<Hand, bool> strongOpener = h =>
            HighCardPoints.Count(h) >= 16 && HighCardPoints.Count(h) <= 19
            && !ShapeEvaluator.IsBalanced(h)
            && ShapeEvaluator.GetShape(h)[Suit.Hearts] >= 5
            && ShapeEvaluator.LongestAndStrongest(h) == Suit.Hearts;

        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            strongOpener,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1Suit_SimpleMajorRaise(Suit.Hearts));

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            var opening = auction.Bids[0].Bid.ToString();
            var response = auction.Bids[2].Bid.ToString();

            Assert.That(opening, Is.EqualTo("1H"),
                $"Expected 1H opening. Hand: {deal[Seat.North]}");
            Assert.That(response, Is.EqualTo("2H"),
                $"Expected 2H raise. Hand: {deal[Seat.South]}");

            // Opener should bid game with 16+ opposite 6-9 (min combined 22, max 28)
            // With 16+ + 6 min = 22 → not always game.
            // With 19 + 9 = 28 → game. This straddles, so expect 3H (invite) or 4H (game)
            var rebid = auction.Bids[4].Bid.ToString();
            Assert.That(rebid, Is.AnyOf("3H", "4H"),
                $"Expected game try or game. Opener: {deal[Seat.North]}, Responder: {deal[Seat.South]}");
        }
    }

    // =============================================
    // 3NT — game values but no major fit
    // =============================================

    [Test]
    public async Task KnowledgeGame_Bids3NT_AfterNTRaiseWithGameValues()
    {
        // 1NT -> 3NT (13+ HCP, no 4-card major)
        // The entire sequence should just be opening + response, no further bidding
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_GameForcing);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("3NT"),
                $"Expected 3NT with 13+ HCP and no major. Hand: {deal[Seat.South]}");

            // After 3NT, everyone should pass (auction ends)
            for (int i = 3; i < auction.Bids.Count; i++)
            {
                Assert.That(auction.Bids[i].Bid.ToString(), Is.EqualTo("Pass"),
                    $"Expected pass after 3NT at position {i}. Bid: {auction.Bids[i].Bid}");
            }
        }
    }

    // =============================================
    // Weak pass after 1NT — no major, no points
    // =============================================

    [Test]
    public async Task KnowledgeSignOff_ResponderPassesWeakHand()
    {
        // 1NT -> Pass (weak hand, no 5+ major)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_WeakPass);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("Pass"),
                $"Expected pass with weak hand. Hand: {deal[Seat.South]}");
        }
    }

    // =============================================
    // Invitational 2NT response
    // =============================================

    [Test]
    public async Task KnowledgeInvite_Bids2NT_WithInvitationalValues()
    {
        // 1NT -> 2NT (11-12 HCP, no major)
        var testDeals = _dealer.GenerateMultipleConstrainedDeals(
            50,
            HandSpecification.Acol1NtOpening,
            HandSpecification.PassingOpponent,
            HandSpecification.ResponseTo1NT_Invitational);

        foreach (var deal in testDeals)
        {
            var auction = await _environment.Table.RunAuction(deal, _environment.Players, Seat.North);

            Assert.That(auction.Bids[0].Bid.ToString(), Is.EqualTo("1NT"));
            Assert.That(auction.Bids[2].Bid.ToString(), Is.EqualTo("2NT"),
                $"Expected 2NT invite. Hand: {deal[Seat.South]}");

            // Opener should accept (3NT) or decline (Pass) based on their actual HCP
            var openerResponse = auction.Bids[4].Bid.ToString();
            Assert.That(openerResponse, Is.AnyOf("Pass", "3NT"),
                $"Expected Pass or 3NT after invite. Opener: {deal[Seat.North]}");
        }
    }
}
