using BridgeIt.Core.Analysis.Auction;
using BridgeIt.Core.Analysis.Hands;
using BridgeIt.Core.Analysis.Partnership;
using BridgeIt.Core.BiddingEngine.Core;
using BridgeIt.Core.BiddingEngine.Rules.Knowledge;
using BridgeIt.Core.Domain.Bidding;
using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Tests.Rules;

[TestFixture]
public class KnowledgeRuleTests
{
    // =============================================
    // Helpers
    // =============================================

    /// <summary>
    /// Creates a DecisionContext simulating a mid-auction scenario where
    /// partner has opened and we're deciding our next action based on
    /// accumulated knowledge.
    /// </summary>
    private static DecisionContext CreateKnowledgeContext(
        int myHcp,
        Dictionary<Suit, int> myShape,
        int partnerHcpMin,
        int partnerHcpMax,
        Dictionary<Suit, int>? partnerMinShape = null,
        Dictionary<Suit, int>? partnerMaxShape = null,
        Bid? currentContract = null,
        Bid? openingBid = null,
        Seat mySeat = Seat.South)
    {
        // Build a plausible auction
        var partnerSeat = mySeat.GetPartner();
        var rhoSeat = partnerSeat.GetNextSeat(); // Seat between partner and me
        var history = new AuctionHistory(partnerSeat);
        var open = openingBid ?? Bid.SuitBid(1, Suit.Hearts);
        history.Add(new AuctionBid(partnerSeat, open));
        history.Add(new AuctionBid(rhoSeat, Bid.Pass())); // RHO passes

        // If there's a current contract beyond the opening, simulate:
        // mySeat bids → LHO passes → partner rebids the contract → RHO passes
        // so it's mySeat's turn again with the contract in place
        if (currentContract != null && currentContract != open)
        {
            var lhoSeat = mySeat.GetNextSeat(); // LHO
            history.Add(new AuctionBid(mySeat, Bid.Pass()));        // I pass first
            history.Add(new AuctionBid(lhoSeat, Bid.Pass()));       // LHO passes
            history.Add(new AuctionBid(partnerSeat, currentContract)); // Partner sets the contract
            history.Add(new AuctionBid(rhoSeat, Bid.Pass()));       // RHO passes
            // Now it's mySeat's turn again
        }

        var handEval = new HandEvaluation
        {
            Hcp = myHcp,
            Shape = myShape,
            IsBalanced = myShape.Values.All(v => v >= 2) && myShape.Values.Count(v => v <= 3) >= 2,
            Losers = 7,
            LongestAndStrongest = myShape.OrderByDescending(kv => kv.Value).First().Key,
            SuitStoppers = new Dictionary<Suit, bool>
            {
                { Suit.Spades, true }, { Suit.Hearts, true },
                { Suit.Diamonds, true }, { Suit.Clubs, true }
            }
        };

        var aucEval = AuctionEvaluator.Evaluate(history);
        var tableKnowledge = new TableKnowledge(mySeat);
        tableKnowledge.Partner.HcpMin = partnerHcpMin;
        tableKnowledge.Partner.HcpMax = partnerHcpMax;

        if (partnerMinShape != null)
        {
            foreach (var kv in partnerMinShape)
                tableKnowledge.Partner.MinShape[kv.Key] = kv.Value;
        }
        if (partnerMaxShape != null)
        {
            foreach (var kv in partnerMaxShape)
                tableKnowledge.Partner.MaxShape[kv.Key] = kv.Value;
        }

        var ctx = new BiddingContext(new Hand(new List<Card>()), history, mySeat, Vulnerability.None);
        return new DecisionContext(ctx, handEval, aucEval, tableKnowledge);
    }

    private static Dictionary<Suit, int> Shape(int s, int h, int d, int c) => new()
    {
        { Suit.Spades, s }, { Suit.Hearts, h }, { Suit.Diamonds, d }, { Suit.Clubs, c }
    };

    // =============================================
    // KnowledgeBidGameInSuit
    // =============================================

    [Test]
    public void GameInSuit_BidsGame_WhenFitAndGameValues()
    {
        // Partner opened 1H (12-19 HCP, 4+ hearts). I have 14 HCP and 4 hearts.
        // Combined min = 14+12 = 26 >= 25 → BidGame. Confirmed fit in hearts.
        var rule = new KnowledgeBidGameInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Hearts)));
    }

    [Test]
    public void GameInSuit_DoesNotFire_WhenNoFit()
    {
        // 14 HCP but only 2 hearts — no fit with partner's 4
        var rule = new KnowledgeBidGameInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(4, 2, 4, 3),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 } },
            partnerMaxShape: new() { { Suit.Hearts, 5 } });

        // 2+5=7 max, so no confirmed fit
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void GameInSuit_DoesNotFire_WhenSignOff()
    {
        // Only 8 HCP — combined max = 8+19 = 27, min = 8+12 = 20 < 25 → SignOff
        var rule = new KnowledgeBidGameInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 8,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 14,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void GameInSuit_PrefersMajorOverMinor()
    {
        // Fit in both hearts and diamonds, game values
        var rule = new KnowledgeBidGameInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(2, 4, 5, 2),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 }, { Suit.Diamonds, 4 } });

        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(4, Suit.Hearts)));
    }

    // =============================================
    // KnowledgeBidGameInNT
    // =============================================

    [Test]
    public void GameInNT_Bids3NT_WhenGameValuesNoMajorFit()
    {
        // 14 HCP + partner 12-19, no major fit
        var rule = new KnowledgeBidGameInNT();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(3, 3, 4, 3),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 } },
            partnerMaxShape: new() { { Suit.Spades, 3 }, { Suit.Hearts, 5 } });

        // Hearts: 3+5 = 8 max but 3+4 = 7 min → no confirmed fit
        // Spades: 3+3 = 6 max → no fit
        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.NoTrumpsBid(3)));
    }

    [Test]
    public void GameInNT_DoesNotFire_WhenMajorFitExists()
    {
        // Confirmed spade fit — should use suit game instead
        var rule = new KnowledgeBidGameInNT();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(5, 2, 4, 2),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Spades, 3 } });

        // Spades: 5+3 = 8 confirmed fit
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void GameInNT_DoesNotFire_WhenSignOff()
    {
        var rule = new KnowledgeBidGameInNT();
        var ctx = CreateKnowledgeContext(
            myHcp: 8,
            myShape: Shape(3, 3, 4, 3),
            partnerHcpMin: 12, partnerHcpMax: 14);

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================
    // KnowledgeInviteInSuit
    // =============================================

    [Test]
    public void InviteInSuit_Bids3M_WhenInviteAndFit()
    {
        // 12 HCP + partner 12-14: min=24, max=26 straddles 25 → Invite
        var rule = new KnowledgeInviteInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 12,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 14,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.SuitBid(3, Suit.Hearts)));
    }

    [Test]
    public void InviteInSuit_DoesNotFire_WhenGameValues()
    {
        // Combined min >= 25 → BidGame, not Invite
        var rule = new KnowledgeInviteInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void InviteInSuit_DoesNotFire_WhenSignOff()
    {
        var rule = new KnowledgeInviteInSuit();
        var ctx = CreateKnowledgeContext(
            myHcp: 6,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 14,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        // max = 6+14 = 20 < 25 → SignOff
        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================
    // KnowledgeSignOffInFit
    // =============================================

    [Test]
    public void SignOffInFit_CorrectsSuit_WhenFitExistsButContractElsewhere()
    {
        // Partner opened 1H, we have fit but weak hand. Current contract is 1NT.
        // Should correct to 2H.
        var rule = new KnowledgeSignOffInFit();
        var ctx = CreateKnowledgeContext(
            myHcp: 6,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 14,
            partnerMinShape: new() { { Suit.Hearts, 4 } },
            currentContract: Bid.NoTrumpsBid(1));

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        var bid = rule.Apply(ctx);
        Assert.That(bid, Is.EqualTo(Bid.SuitBid(2, Suit.Hearts)));
    }

    [Test]
    public void SignOffInFit_DoesNotFire_WhenAlreadyInFitSuit()
    {
        // Current contract is already 2H (our fit suit) — just pass
        var rule = new KnowledgeSignOffInFit();
        var ctx = CreateKnowledgeContext(
            myHcp: 6,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 14,
            partnerMinShape: new() { { Suit.Hearts, 4 } },
            currentContract: Bid.SuitBid(2, Suit.Hearts));

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    [Test]
    public void SignOffInFit_DoesNotFire_WhenGameValues()
    {
        var rule = new KnowledgeSignOffInFit();
        var ctx = CreateKnowledgeContext(
            myHcp: 14,
            myShape: Shape(3, 4, 3, 3),
            partnerHcpMin: 12, partnerHcpMax: 19,
            partnerMinShape: new() { { Suit.Hearts, 4 } });

        Assert.That(rule.CouldMakeBid(ctx), Is.False);
    }

    // =============================================
    // KnowledgeSignOff
    // =============================================

    [Test]
    public void SignOff_AlwaysApplies_Passes()
    {
        var rule = new KnowledgeSignOff();
        var ctx = CreateKnowledgeContext(
            myHcp: 6,
            myShape: Shape(3, 3, 4, 3),
            partnerHcpMin: 12, partnerHcpMax: 14);

        Assert.That(rule.CouldMakeBid(ctx), Is.True);
        Assert.That(rule.Apply(ctx), Is.EqualTo(Bid.Pass()));
    }

    [Test]
    public void SignOff_ExplainsBid_OnlyForPass()
    {
        var rule = new KnowledgeSignOff();
        var ctx = CreateKnowledgeContext(
            myHcp: 6,
            myShape: Shape(3, 3, 4, 3),
            partnerHcpMin: 12, partnerHcpMax: 14);

        Assert.That(rule.CouldExplainBid(Bid.Pass(), ctx), Is.True);
        Assert.That(rule.CouldExplainBid(Bid.SuitBid(2, Suit.Hearts), ctx), Is.False);
    }

    // =============================================
    // Priority ordering — verify knowledge rules sit below pattern rules
    // =============================================

    [Test]
    public void KnowledgeRules_HaveLowerPriorityThanPatternRules()
    {
        var gameInSuit = new KnowledgeBidGameInSuit();
        var gameInNT = new KnowledgeBidGameInNT();
        var invite = new KnowledgeInviteInSuit();
        var signOffFit = new KnowledgeSignOffInFit();
        var signOff = new KnowledgeSignOff();

        // All knowledge rules should be priority <= 2
        Assert.That(gameInSuit.Priority, Is.LessThanOrEqualTo(2));
        Assert.That(gameInNT.Priority, Is.LessThanOrEqualTo(2));
        Assert.That(invite.Priority, Is.LessThanOrEqualTo(2));
        Assert.That(signOffFit.Priority, Is.LessThanOrEqualTo(2));
        Assert.That(signOff.Priority, Is.EqualTo(0));

        // SignOff should be the lowest
        Assert.That(signOff.Priority, Is.LessThan(signOffFit.Priority));
        Assert.That(signOff.Priority, Is.LessThan(gameInNT.Priority));
    }
}
