using BridgeIt.Core.Analysis.Hand;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Analysis.Hand;

[TestFixture]
public class LosingTrickCountTests
{
    [Test]
    [TestCase("SA", 0, Description = "No losers - Ace only")]
    [TestCase("SAK", 0, Description = "No losers - Ace and King")]
    [TestCase("SAKQ", 0, Description = "No losers - Ace, King and Queen")]
    [TestCase("SAKQ432", 0, Description = "No losers- hold top honours")]

    public void CountSuitLosers_CalculatesLosersInSuitCorrectly(string handString, int expectedPoints)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedPoints));
    }
    #region CountSuitLosers Tests

    [Test]
    [TestCase("", 0, Description = "Empty suit - no losers")]
    public void CountSuitLosers_EmptySuit_ReturnsZero(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SA", 0, Description = "Singleton Ace - no losers")]
    [TestCase("SK", 1, Description = "Singleton King - 1 loser")]
    [TestCase("SQ", 1, Description = "Singleton Queen - 1 loser")]
    [TestCase("SJ", 1, Description = "Singleton Jack - 1 loser")]
    [TestCase("S2", 1, Description = "Singleton small card - 1 loser")]
    public void CountSuitLosers_SingletonCards_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAK", 0, Description = "Doubleton AK - no losers")]
    [TestCase("SAQ", 1, Description = "Doubleton AQ - 1 loser")]
    [TestCase("SA2", 1, Description = "Doubleton Ax - 1 loser")]
    [TestCase("SKQ", 1, Description = "Doubleton KQ - 1 loser")]
    [TestCase("SK2", 1, Description = "Doubleton Kx - 1 losers")]
    [TestCase("SQ2", 2, Description = "Doubleton Qx - 2 losers")]
    [TestCase("S32", 2, Description = "Doubleton xx - 2 losers")]
    public void CountSuitLosers_DoubletonCards_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAKQ", 0, Description = "AKQ - no losers")]
    [TestCase("SAKJ", 1, Description = "AKJ - 1 loser")]
    [TestCase("SAK2", 1, Description = "AKx - 1 loser")]
    [TestCase("SAQ2", 1, Description = "AQx - 1 loser")]
    [TestCase("SA32", 2, Description = "Axx - 2 losers")]
    [TestCase("SKQ2", 1, Description = "KQx - 1 loser")]
    [TestCase("SKJ2", 2, Description = "KJx - 2 losers")]
    [TestCase("SK32", 2, Description = "Kxx - 2 losers")]
    [TestCase("SQJ2", 2, Description = "QJx - 2 losers")]
    [TestCase("SQ32", 2, Description = "Qxx - 3 losers")]
    [TestCase("SJ32", 3, Description = "Jxx - 3 losers")]
    [TestCase("S432", 3, Description = "xxx - 3 losers")]
    public void CountSuitLosers_ThreeCardSuits_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAKQJ", 0, Description = "AKQJ - no losers")]
    [TestCase("SAKQ2", 0, Description = "AKQx - no losers")]
    [TestCase("SAKJ2", 1, Description = "AKJx - 1 loser")]
    [TestCase("SAK32", 1, Description = "AKxx - 1 loser")]
    [TestCase("SAQJ2", 1, Description = "AQJx - 1 loser")]
    [TestCase("SAQ32", 1, Description = "AQxx - 2 losers")]
    [TestCase("SA432", 2, Description = "Axxx - 2 losers")]
    [TestCase("SKQJ2", 1, Description = "KQJx - 1 loser")]
    [TestCase("SKQ32", 1, Description = "KQxx - 2 losers")]
    [TestCase("SK432", 2, Description = "Kxxx - 2 losers")]
    [TestCase("SQJ32", 2, Description = "QJxx - 2 losers")]
    [TestCase("SQ432", 2, Description = "Qxxx - 3 losers")]
    [TestCase("SJ432", 3, Description = "Jxxx - 3 losers")]
    [TestCase("S5432", 3, Description = "xxxx - 3 losers")]
    public void CountSuitLosers_FourPlusCardSuits_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAKQJ98765432", 0, Description = "Long suit with AKQ - no losers")]
    [TestCase("SAKJ98765432", 1, Description = "Long suit with AKJ - 1 loser")]
    [TestCase("SA98765432", 2, Description = "Long suit with A only - 2 losers")]
    [TestCase("SKQJ98765432", 1, Description = "Long suit with KQJ - 1 loser")]
    [TestCase("S98765432", 3, Description = "Long suit with no honors - 3 losers")]
    public void CountSuitLosers_LongSuits_CapsAtThreeLosers(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var suitCards = hand.Cards.ToArray();
        var result = LosingTrickCount.CountSuitLosers(suitCards);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    #endregion

    #region Count (Full Hand) Tests

    [Test]
    [TestCase("SA SK SQ SJ", 0, Description = "All singletons - 12 losers (worst hand)")]
    [TestCase("SAKQJ HAKQJ DAKQJ CAKQJ", 0, Description = "Perfect hand - no losers")]
    [TestCase("SAKQ HAKQ DAKQ CAKQ", 0, Description = "All suits AKQ - no losers")]
    public void Count_FullHand_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAKQJ H32 D32 C32", 6, Description = "One strong suit, three weak suits")]
    [TestCase("SAK HAK DAK CAK", 0, Description = "All suits AK doubleton")]
    [TestCase("SA HA DA CA", 0, Description = "All singleton Aces")]
    [TestCase("SK HK DK CK", 4, Description = "All singleton Kings")]
    public void Count_BalancedHands_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SAKQJ98 HA D32 C32", 4, Description = "Long strong suit with voids")]
    [TestCase("SAKQJ9876 HA D C32", 2, Description = "7-card suit with void")]
    [TestCase("SAKQJ9876 HA D C", 0, Description = "7-card suit with two voids")]
    public void Count_UnbalancedHands_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("SA432 HK432 DQ32 CJ32", 9, Description = "Each suit has one honor")]
    [TestCase("SAK32 HK32 DQ32 C432", 8, Description = "Mixed hand")]
    [TestCase("SAKQ2 H432 D432 C432", 9, Description = "One very strong suit")]
    public void Count_RealisticHands_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    [Test]
    [TestCase("   ", 0, Description = "Void hand (all four suits empty)")]
    [TestCase("SAKQJ   ", 0, Description = "Hand with three voids")]
    [TestCase("SAK H D C", 0, Description = "AK doubleton with three voids")]
    public void Count_HandsWithVoids_CalculatesCorrectly(string handString, int expectedLosers)
    {
        var hand = handString.ParseHand();
        var result = LosingTrickCount.Count(hand);
        Assert.That(result, Is.EqualTo(expectedLosers));
    }

    #endregion
}