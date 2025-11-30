using BridgeIt.Core.Analysis.Hand;
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