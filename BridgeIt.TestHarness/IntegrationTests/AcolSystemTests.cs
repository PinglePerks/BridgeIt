// using NUnit.Framework;
// using BridgeIt.TestHarness.Setup;
// using BridgeIt.Core.Domain.Primatives; // For ParseHand
//
// namespace BridgeIt.TestHarness.IntegrationTests;
//
// [TestFixture]
// public class AcolSystemTests
// {
//     private TestBridgeEnvironment _environment;
//     
//     // Path to your YAML files (adjust as needed for your machine/project structure)
//     private const string AcolRulesPath = "../../../../BridgeIt.CLI/BiddingRules"; 
//
//     [OneTimeSetUp]
//     public void SetupSystem()
//     {
//         // 1. Build the Environment ONCE for all tests (faster)
//         _environment = TestBridgeEnvironment.Create()
//             .WithRulesFromDirectory(AcolRulesPath);
//             
//         // Check we actually loaded rules
//         // Assert.That(_environment.Engine.RuleCount, Is.GreaterThan(0)); 
//     }
//
//     [Test]
//     [TestCaseSource(typeof(HandScenarios), nameof(HandScenarios.UnbalancedScenarios))]
//     public void RunScenario_MajorSuitFit(string northHandStr, string southHandStr, string expectedBid1,
//         string expectedBid2)
//     {
//         
//         var handN = ParseHand(northHandStr);
//         var handS = ParseHand(southHandStr);
//         
//         // Create a full deal (E/W get remaining cards)
//         var deal = DealCards(handN, handS);
//
//         // Act
//         // Run the auction starting with North
//         Console.WriteLine("\n--- Hands Dealt ---");
//         foreach(var seat in deal.Keys) Console.WriteLine($"{seat}: {deal[seat]}");
//         Console.WriteLine("-------------------\n");
//         
//         var auction = _environment.Table.RunAuction(deal, Seat.North);
//         
//         Console.WriteLine("Final Auction Decisions:");
//         foreach (var decision in auction)
//         {
//             Console.WriteLine($"Bid: {decision.ChosenBid,-5} | {decision.Explanation}");
//         }
//     }
//     
//
//     [Test]
//     [TestCaseSource(typeof(HandScenarios), nameof(HandScenarios.MajorSuitFitScenarios))]
//     [TestCaseSource(typeof(HandScenarios), nameof(HandScenarios.HighPointScenarios))]
//     [TestCaseSource(typeof(HandScenarios), nameof(HandScenarios.UnbalancedScenarios))]
//     [TestCaseSource(typeof(HandScenarios), nameof(HandScenarios.NoTrumpScenarios))]
//     public void RunScenario(string northHandStr, string southHandStr, string expectedBid1, string expectedBid2)
//     {
//         // Arrange
//         var handN = ParseHand(northHandStr);
//         var handS = ParseHand(southHandStr);
//         
//         // Create a full deal (E/W get remaining cards)
//         var deal = DealCards(handN, handS);
//
//         // Act
//         // Run the auction starting with North
//         var auction = _environment.Table.RunAuction(deal, Seat.North);
//
//         // Assert
//         // We expect the auction to start: North Bid -> East Pass -> South Bid
//         // Note: This assumes East passes. If your engine makes East bid, this logic needs adjusting.
//         
//         Assert.That(auction.Count, Is.GreaterThanOrEqualTo(3), "Auction ended too early");
//         
//         var bid1 = auction[0].ChosenBid.ToString(); // North
//         var bid2 = auction[2].ChosenBid.ToString(); // South (Index 2 because Index 1 is East)
//
//         Assert.Multiple(() =>
//         {
//             Assert.That(bid1, Is.EqualTo(expectedBid1), $"North (Opener) failed. Hand: {northHandStr}");
//             Assert.That(bid2, Is.EqualTo(expectedBid2), $"South (Responder) failed. Sequence: {bid1} -> Pass -> ?");
//         });
//     }
//
//     // --- Helpers ---
//
//     private Dictionary<Seat, Hand> DealCards(Hand north, Hand south)
//     {
//         var deck = new Deck();
//         var usedCards = new HashSet<string>(north.Cards.Select(c => c.ToString())
//             .Concat(south.Cards.Select(c => c.ToString())));
//
//         var remaining = deck.Cards
//             .Where(c => !usedCards.Contains(c.ToString()))
//             .ToList();
//
//         return new Dictionary<Seat, Hand>
//         {
//             { Seat.North, north },
//             { Seat.South, south },
//             { Seat.East, new Hand(remaining.Take(13)) },
//             { Seat.West, new Hand(remaining.Skip(13).Take(13)) }
//         };
//     }
//
//     private Hand ParseHand(string handStr)
//     {
//         // Reuse your string parsing logic here or call your extension method
//         // Assuming extension method is visible:
//         // return handStr.ParseHand();
//         
//         // If extension isn't visible, paste the helper logic here:
//         var parts = handStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//         var cards = new List<Card>();
//         foreach (var part in parts)
//         {
//             var suit = part[0].ToString().ToSuit();
//             foreach (var r in part.Substring(1)) 
//                 cards.Add(($"{r}{suit.ShortName()}".ParseCard())); // e.g. "KS"
//         }
//         return new Hand(cards);
//     }
// }