using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Extensions;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Hand;

[TestFixture]
public class ShapeEvaluatorTests
{
    // --- GetShape Tests ---

    [Test]
    [Description("Verifies that GetShape returns the exact count of cards for each suit.")]
    public void GetShape_ReturnsCorrectCounts_ForMixedHand()
    {
        // Arrange: 4 Spades, 3 Hearts, 4 Diamonds, 2 Clubs
        var hand = CreateHandWithShape(4, 3, 4, 2);

        // Act
        var shape = ShapeEvaluator.GetShape(hand);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(shape[Suit.Spades], Is.EqualTo(4), "Spades count incorrect");
            Assert.That(shape[Suit.Hearts], Is.EqualTo(3), "Hearts count incorrect");
            Assert.That(shape[Suit.Diamonds], Is.EqualTo(4), "Diamonds count incorrect");
            Assert.That(shape[Suit.Clubs], Is.EqualTo(2), "Clubs count incorrect");
        });
    }

    [Test]
    [Description("Verifies that GetShape handles voids (0 count) correctly.")]
    public void GetShape_ReturnsZero_ForVoidSuit()
    {
        // Arrange: 5 Spades, 5 Hearts, 3 Diamonds, 0 Clubs (Void)
        var hand = CreateHandWithShape(5, 5, 3, 0);

        // Act
        var shape = ShapeEvaluator.GetShape(hand);

        // Assert
        Assert.That(shape[Suit.Clubs], Is.EqualTo(0));
    }

    // --- IsBalanced Tests ---
    // Definition: No singleton, no void. Usually 4333, 4432, 5332.

    [Test]
    [TestCase(4, 3, 3, 3, Description = "4-3-3-3 (Flat) is Balanced")]
    [TestCase(3, 4, 3, 3, Description = "3-4-3-3 (Flat) is Balanced")]
    public void IsBalanced_ReturnsTrue_ForFlatHands(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.True);
    }

    [Test]
    [TestCase(4, 4, 3, 2, Description = "4-4-3-2 is Balanced")]
    [TestCase(2, 4, 4, 3, Description = "2-4-4-3 is Balanced")]
    public void IsBalanced_ReturnsTrue_ForStandardBalanced(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.True);
    }

    [Test]
    [TestCase(5, 3, 3, 2, Description = "5-3-3-2 is Balanced (Modern Standard)")]
    [TestCase(2, 3, 3, 5, Description = "2-3-3-5 is Balanced")]
    public void IsBalanced_ReturnsTrue_ForFiveCardMajorBalanced(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.True);
    }

    [Test]
    [TestCase(5, 4, 2, 2, Description = "5-4-2-2 is Semi-Balanced, NOT Balanced")]
    [TestCase(6, 3, 2, 2, Description = "6-3-2-2 is Semi-Balanced, NOT Balanced")]
    public void IsBalanced_ReturnsFalse_ForSemiBalancedShapes(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.False);
    }

    [Test]
    [TestCase(5, 4, 3, 1, Description = "Any singleton makes hand Unbalanced")]
    [TestCase(4, 4, 4, 1, Description = "4-4-4-1 is Unbalanced")]
    [TestCase(6, 3, 3, 1, Description = "6-3-3-1 is Unbalanced")]
    public void IsBalanced_ReturnsFalse_ForHandsWithSingleton(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.False);
    }

    [Test]
    [TestCase(5, 5, 3, 0, Description = "Any void makes hand Unbalanced")]
    public void IsBalanced_ReturnsFalse_ForHandsWithVoid(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsBalanced(hand), Is.False);
    }

    // --- IsSemiBalanced Tests ---
    // Definition: Often 5-4-2-2 or 6-3-2-2.

    [Test]
    [TestCase(5, 4, 2, 2, Description = "5-4-2-2 is Semi-Balanced")]
    [TestCase(2, 2, 5, 4, Description = "2-2-5-4 is Semi-Balanced")]
    public void IsSemiBalanced_ReturnsTrue_For5422(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsSemiBalanced(hand), Is.True);
    }

    [Test]
    [TestCase(6, 3, 2, 2, Description = "6-3-2-2 is Semi-Balanced")]
    [TestCase(2, 6, 2, 3, Description = "2-6-2-3 is Semi-Balanced")]
    public void IsSemiBalanced_ReturnsTrue_For6322(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsSemiBalanced(hand), Is.True);
    }

    [Test]
    [TestCase(4, 3, 3, 3, Description = "Balanced hand is not Semi-Balanced")]
    [TestCase(4, 4, 3, 2, Description = "Balanced hand is not Semi-Balanced")]
    public void IsSemiBalanced_ReturnsFalse_ForTrulyBalancedHands(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsSemiBalanced(hand), Is.False);
    }

    [Test]
    [TestCase(5, 4, 3, 1, Description = "Singleton makes it Unbalanced, not Semi-Balanced")]
    [TestCase(5, 5, 2, 1, Description = "5-5-2-1 is Unbalanced")]
    [TestCase(6, 4, 2, 1, Description = "6-4-2-1 is Unbalanced")]
    public void IsSemiBalanced_ReturnsFalse_ForUnbalancedHands(int s, int h, int d, int c)
    {
        var hand = CreateHandWithShape(s, h, d, c);
        Assert.That(ShapeEvaluator.IsSemiBalanced(hand), Is.False);
    }

    // --- LongestAndStrongest Tests ---

    [Test]
    public void LongestAndStrongest_ReturnsLongestSuit()
    {
        var hand = CreateHandWithShape(5, 3, 3, 2);
        Assert.That(ShapeEvaluator.LongestAndStrongest(hand), Is.EqualTo(Suit.Spades));
    }

    [Test]
    public void LongestAndStrongest_TieBreaksHighestRanking()
    {
        // 4-4-3-2 — spades and hearts tied, spades wins (higher ranking)
        var hand = CreateHandWithShape(4, 4, 3, 2);
        Assert.That(ShapeEvaluator.LongestAndStrongest(hand), Is.EqualTo(Suit.Spades));
    }

    // --- SuitsWithMinLength Tests ---

    [Test]
    public void SuitsWithMinLength_ReturnsAllSuitsAboveThreshold()
    {
        // 5-4-3-1 shape
        var hand = CreateHandWithShape(5, 4, 3, 1);
        var result = ShapeEvaluator.SuitsWithMinLength(hand, 4);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Suit.Spades), "Longest first");
        Assert.That(result[1], Is.EqualTo(Suit.Hearts), "Second longest");
    }

    [Test]
    public void SuitsWithMinLength_ReturnsEmpty_WhenNoneMeetThreshold()
    {
        var hand = CreateHandWithShape(4, 3, 3, 3);
        var result = ShapeEvaluator.SuitsWithMinLength(hand, 5);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SuitsWithMinLength_OrdersByLengthThenRank()
    {
        // 5S, 5H, 2D, 1C — both 5-card suits, spades first (higher ranking)
        var hand = CreateHandWithShape(5, 5, 2, 1);
        var result = ShapeEvaluator.SuitsWithMinLength(hand, 5);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[1], Is.EqualTo(Suit.Hearts));
    }

    [Test]
    public void SuitsWithMinLength_6CardSuitBeforeTwoFiveCardSuits()
    {
        // 6D, 5S, 1H, 1C
        var hand = CreateHandWithShape(5, 1, 6, 1);
        var result = ShapeEvaluator.SuitsWithMinLength(hand, 5);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Suit.Diamonds), "6 cards comes first");
        Assert.That(result[1], Is.EqualTo(Suit.Spades), "5 cards second");
    }

    [Test]
    public void SuitsWithMinLength_MinLength4_IncludesAllFourCardPlusSuits()
    {
        // 4-4-4-1
        var hand = CreateHandWithShape(4, 4, 4, 1);
        var result = ShapeEvaluator.SuitsWithMinLength(hand, 4);

        Assert.That(result, Has.Count.EqualTo(3));
        // All equal length — ordered by rank descending
        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[1], Is.EqualTo(Suit.Hearts));
        Assert.That(result[2], Is.EqualTo(Suit.Diamonds));
    }

    // --- SuitsByLengthDescending Tests ---

    [Test]
    public void SuitsByLengthDescending_ReturnsAllFourSuits()
    {
        var hand = CreateHandWithShape(5, 3, 3, 2);
        var result = ShapeEvaluator.SuitsByLengthDescending(hand);
        Assert.That(result, Has.Count.EqualTo(4));
    }

    [Test]
    public void SuitsByLengthDescending_OrdersCorrectly()
    {
        // 5S, 4H, 3D, 1C
        var hand = CreateHandWithShape(5, 4, 3, 1);
        var result = ShapeEvaluator.SuitsByLengthDescending(hand);

        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[1], Is.EqualTo(Suit.Hearts));
        Assert.That(result[2], Is.EqualTo(Suit.Diamonds));
        Assert.That(result[3], Is.EqualTo(Suit.Clubs));
    }

    [Test]
    public void SuitsByLengthDescending_TieBreaksHighestRankFirst()
    {
        // 4S, 4H, 3D, 2C — spades and hearts tied
        var hand = CreateHandWithShape(4, 4, 3, 2);
        var result = ShapeEvaluator.SuitsByLengthDescending(hand);

        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[1], Is.EqualTo(Suit.Hearts));
    }

    // --- HandEvaluation convenience method tests ---

    [Test]
    public void HandEvaluation_SuitsWithMinLength_MatchesShapeEvaluator()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 1 } };
        var eval = new HandEvaluation { Shape = shape, Hcp = 12, IsBalanced = false, Losers = 7, LongestAndStrongest = Suit.Spades };

        var result = eval.SuitsWithMinLength(4);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[1], Is.EqualTo(Suit.Hearts));
    }

    [Test]
    public void HandEvaluation_SuitsByLengthDescending_ReturnsAllFour()
    {
        var shape = new Dictionary<Suit, int>
            { { Suit.Spades, 5 }, { Suit.Hearts, 4 }, { Suit.Diamonds, 3 }, { Suit.Clubs, 1 } };
        var eval = new HandEvaluation { Shape = shape, Hcp = 12, IsBalanced = false, Losers = 7, LongestAndStrongest = Suit.Spades };

        var result = eval.SuitsByLengthDescending();
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0], Is.EqualTo(Suit.Spades));
        Assert.That(result[3], Is.EqualTo(Suit.Clubs));
    }

    // --- GetStoppers / EvaluateStopperQuality Tests ---

    [Test]
    [Description("Ace is always a full stopper, even as singleton")]
    public void EvaluateStopperQuality_Ace_IsFull()
    {
        // SA HKQ532 DJT97 C42 — singleton ace of spades
        var hand = "SA HKQ532 DJT97 C42".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Full));
    }

    [Test]
    [Description("Kx is a full stopper")]
    public void EvaluateStopperQuality_KingWithGuard_IsFull()
    {
        // SK2 HAQJ9 DT873 C65
        var hand = "SK2 HAQJ9 DT873 C65".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Full));
    }

    [Test]
    [Description("Qxx is a full stopper")]
    public void EvaluateStopperQuality_QueenWithTwoGuards_IsFull()
    {
        // SQ52 HAK98 DJT73 C64
        var hand = "SQ52 HAK98 DJT73 C64".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Full));
    }

    [Test]
    [Description("Jxxx is a full stopper")]
    public void EvaluateStopperQuality_JackWithThreeGuards_IsFull()
    {
        // SJ532 HAK98 DQT7 C6
        var hand = "SJ532 HAK98 DQT7 C6".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Full));
    }

    [Test]
    [Description("Singleton King is a partial stopper")]
    public void EvaluateStopperQuality_SingletonKing_IsPartial()
    {
        // SK HAQ9876 DJT3 C42
        var hand = "SK HAQ9876 DJT3 C42".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Partial));
    }

    [Test]
    [Description("Qx is a partial stopper")]
    public void EvaluateStopperQuality_QueenWithOneGuard_IsPartial()
    {
        // SQ2 HAK987 DJT63 C54
        var hand = "SQ2 HAK987 DJT63 C54".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Partial));
    }

    [Test]
    [Description("Jxx is a partial stopper")]
    public void EvaluateStopperQuality_JackWithTwoGuards_IsPartial()
    {
        // SJ52 HAK987 DQT3 C64
        var hand = "SJ52 HAK987 DQT3 C64".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.Partial));
    }

    [Test]
    [Description("Small cards only is no stopper")]
    public void EvaluateStopperQuality_SmallCardsOnly_IsNone()
    {
        // S532 HAKQ9 DJT87 C64
        var hand = "S532 HAKQ9 DJT87 C64".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.None));
    }

    [Test]
    [Description("Void is no stopper")]
    public void EvaluateStopperQuality_Void_IsNone()
    {
        // HAKQ98 DJT732 C654 — void in spades
        var hand = "HAKQ98 DJT732 C654".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.None));
    }

    [Test]
    [Description("Singleton Queen is no stopper")]
    public void EvaluateStopperQuality_SingletonQueen_IsNone()
    {
        // SQ HAK98765 DJT3 C2
        var hand = "SQ HAK98765 DJT3 C2".ToHand();
        Assert.That(ShapeEvaluator.EvaluateStopperQuality(hand, Suit.Spades), Is.EqualTo(StopperQuality.None));
    }

    [Test]
    [Description("GetStoppers returns quality for all four suits")]
    public void GetStoppers_ReturnsAllFourSuits()
    {
        // SAKQ HJ32 D9876 C54 — S=Full, H=Partial(Jxx), D=None, C=None
        var hand = "SAKQ HJ32 D9876 C54".ToHand();
        var stoppers = ShapeEvaluator.GetStoppers(hand);

        Assert.Multiple(() =>
        {
            Assert.That(stoppers[Suit.Spades], Is.EqualTo(StopperQuality.Full));
            Assert.That(stoppers[Suit.Hearts], Is.EqualTo(StopperQuality.Partial));
            Assert.That(stoppers[Suit.Diamonds], Is.EqualTo(StopperQuality.None));
            Assert.That(stoppers[Suit.Clubs], Is.EqualTo(StopperQuality.None));
        });
    }

    // --- Helper for constructing hands by shape ---
    private Core.Domain.Primatives.Hand CreateHandWithShape(int spades, int hearts, int diamonds, int clubs)
    {
        if (spades + hearts + diamonds + clubs != 13)
            throw new ArgumentException("Shape must sum to 13");

        var cards = new List<Card>();
        // Use a fresh deck for source cards
        var deck = new Deck(); 
        var availableCards = deck.Cards.ToList();

        AddSuitToHand(cards, availableCards, Suit.Spades, spades);
        AddSuitToHand(cards, availableCards, Suit.Hearts, hearts);
        AddSuitToHand(cards, availableCards, Suit.Diamonds, diamonds);
        AddSuitToHand(cards, availableCards, Suit.Clubs, clubs);

        return new Core.Domain.Primatives.Hand(cards);
    }

    private void AddSuitToHand(List<Card> hand, List<Card> source, Suit suit, int count)
    {
        var cardsToAdd = source.Where(c => c.Suit == suit).Take(count).ToList();
        if (cardsToAdd.Count < count)
            throw new InvalidOperationException($"Not enough cards in deck for {suit}");
        
        hand.AddRange(cardsToAdd);
    }
}