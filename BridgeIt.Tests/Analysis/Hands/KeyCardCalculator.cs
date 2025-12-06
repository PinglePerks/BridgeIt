using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Hands;

[TestFixture]
public class KeyCardCalculatorTests
{
[Test]
        public void Calculate_EmptyHand_ReturnsZero()
        {
            // Arrange
            var hand = new Core.Domain.Primatives.Hand(new List<Card>());
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_HandWithOneAce_ReturnsOne()
        {
            // Arrange
            var cards = new List<Card> { new Card(Suit.Hearts, Rank.Ace) };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_HandWithOneKingOfSpecifiedSuit_ReturnsOne()
        {
            // Arrange
            var cards = new List<Card> { new Card(Suit.Spades, Rank.King) };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_HandWithOneKingOfDifferentSuit_ReturnsZero()
        {
            // Arrange
            var cards = new List<Card> { new Card(Suit.Hearts, Rank.King) };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_HandWithAceAndKingOfSpecifiedSuit_ReturnsTwo()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Spades, Rank.King)
            };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void Calculate_HandWithAceOfSpecifiedSuit_ReturnsOne()
        {
            // Arrange
            var cards = new List<Card> { new Card(Suit.Spades, Rank.Ace) };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void Calculate_HandWithMultipleAcesAndKings_ReturnsCorrectCount()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Spades, Rank.King),
                new Card(Suit.Hearts, Rank.King),
                new Card(Suit.Spades, Rank.Queen) // Non-key card
            };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(3)); // 2 Aces + 1 King of Spades
        }

        [Test]
        public void Calculate_HandWithNoAcesOrRelevantKings_ReturnsZero()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Hearts, Rank.Jack),
                new Card(Suit.Diamonds, Rank.King) // King but wrong suit
            };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Calculate_HandWithDuplicateAces_CountsAll()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Spades, Rank.Ace) // Duplicate, but function counts it
            };
            var hand = new Core.Domain.Primatives.Hand(cards);
            var suit = Suit.Spades;

            // Act
            var result = KeyCardCalculator.Calculate(hand, suit);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void Calculate_DifferentSuits_ReturnsCorrectForEach()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            var hand = new Core.Domain.Primatives.Hand(cards);

            // Act
            var resultSpades = KeyCardCalculator.Calculate(hand, Suit.Spades);
            var resultHearts = KeyCardCalculator.Calculate(hand, Suit.Hearts);

            // Assert
            Assert.That(resultSpades, Is.EqualTo(1)); // Ace only
            Assert.That(resultHearts, Is.EqualTo(2)); // Ace + Hearts King
        }
    }