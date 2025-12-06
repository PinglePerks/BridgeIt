using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Hand;

[TestFixture]
public class HighCardPointsTests
{
       [Test]
    [TestCase("SA", 4, Description = "Ace = 4")]
    [TestCase("SK", 3, Description = "King = 3")]
    [TestCase("SQ", 2, Description = "Queen = 2")]
    [TestCase("SJ", 1, Description = "Jack = 1")]
    [TestCase("ST", 0, Description = "Ten = 0")]
    [TestCase("S9", 0, Description = "Nine = 0")]
    public void Count_CalculatesIndividualHonorsCorrectly(string handString, int expectedPoints)
    {
        var hand = handString.ParseHand();
        var result = HighCardPoints.Count(hand);
        Assert.That(result, Is.EqualTo(expectedPoints));
    }

    // --- COMBINATIONS ---

    [Test]
    [TestCase("HAKQJ", 10, Description = "Solid Suit: 4+3+2+1 = 10")]
    [TestCase("SAT98", 4, Description = "Ace + Spot cards = 4")]
    [TestCase("S23456789", 0, Description = "No Honors = 0")]
    public void Count_CalculatesSuitCombinationsCorrectly(string handString, int expectedPoints)
    {
        var hand = handString.ParseHand();
        var result = HighCardPoints.Count(hand);
        Assert.That(result, Is.EqualTo(expectedPoints));
    }

    // --- FULL HANDS ---

    [Test]
    [Description("Balanced 12 HCP Hand")]
    public void Count_CalculatesFullHand_12HCP()
    {
        // S: K J 4 2 (3+1=4) | H: Q 5 3 (2) | D: K 8 7 2 (3) | C: Q 4 2 (2) = 11?? Wait.
        // S: K(3) J(1) = 4
        // H: Q(2) = 2
        // D: K(3) = 3
        // C: Q(2) = 2
        // Total = 11. Let's adjust the test case string to be exactly 12.
        // Add a Jack to Clubs -> C: Q J 4 (2+1=3). Total 12.
        
        var hand = ("SKJ42 HQ53 DK872 CQJ4").ParseHand();
        var result = HighCardPoints.Count(hand);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    [Description("Maximum Hand (All Aces, Kings, Queens, Jacks)")]
    public void Count_CalculatesMaxPossible_37HCP() // 4*10 - 3 (missing 3 Jacks) = 37?
    {
        // Theoretical max for 13 cards:
        // 4 Aces (16) + 4 Kings (12) + 4 Queens (8) + 1 Jack (1) = 37 HCP
        var hand = ("SAKQJ HAKQ DAKQ CAKQ").ParseHand();
        var result = HighCardPoints.Count(hand);
        Assert.That(result, Is.EqualTo(37));
    }
    
    [Test]
    [Description("Empty Hand should correspond to 0 HCP")]
    public void Count_ReturnsZero_ForEmptyHand()
    {
        var hand = new Core.Domain.Primatives.Hand(new List<Card>());
        var result = HighCardPoints.Count(hand);
        Assert.That(result, Is.EqualTo(0));
    }
    
    [Test]
    [TestCase("HAKQJ", 10)]
    [TestCase("SAT98", 4)]
    public void Count(string handString, int expectedPoints)
    {
        //Arrange
        var hand = handString.ParseHand();
        
        //Act
        var result = HighCardPoints.Count(hand);
        
        //Assert
        Assert.That(result, Is.EqualTo(expectedPoints));
        
    }
    
}