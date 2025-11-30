using NUnit.Framework;

namespace BridgeIt.TestHarness.IntegrationTests;

public class HandScenarios
{
    // ==============================================================================
    // 1. MAJOR SUIT FITS (Opener 1H/1S -> Responder Raises)
    // ==============================================================================
    
    public static IEnumerable<TestCaseData> MajorSuitFitScenarios()
    {
        // A. Simple Raise (1H - 2H)
        // North: 1H Opener (13 HCP, 5 Hearts)
        // South: 6-9 HCP, 3 Hearts
        yield return new TestCaseData(
            "SAK4 HQJ872 DK43 C52",  // North (13 HCP)
            "S872 HK53 DQ76 CJ94",   // South (7 HCP, 3 Hearts)
            "1H", "2H"               // Expected Sequence
        ).SetName("MajorFit_SimpleRaise_1H_2H");

        // B. Limit Raise (1S - 3S)
        // North: 1S Opener (14 HCP, 5 Spades)
        // South: 11 HCP, 4 Spades (Limit Raise values)
        yield return new TestCaseData(
            "SAKJ82 H87 DAQ5 C942",  // North (14 HCP)
            "SQ974 HAK2 DJ83 C865",  // South (11 HCP, 4 Spades)
            "1S", "3S"
        ).SetName("MajorFit_LimitRaise_1S_3S");

        // C. Game Force (1H - 4H)
        // North: 1H Opener
        // South: 13 HCP, 4 Hearts
        yield return new TestCaseData(
            "S74 HAQJ92 DKJ5 CQ82",  // North (13 HCP)
            "SKQ5 HK876 DAQ4 C763",  // South (14 HCP)
            "1H", "4H"               // Direct Game or Checkback sequence depending on rules
        ).SetName("MajorFit_GameForce_1H_4H");
    }

    // ==============================================================================
    // 2. HIGH POINTS (Slams and Strong Openers)
    // ==============================================================================

    public static IEnumerable<TestCaseData> HighPointScenarios()
    {
        // A. Strong 2C Opening (23+ HCP Balanced)
        yield return new TestCaseData(
            "SAKQJ HAKQJ DAK C432", // North (25 HCP)
            "S432 H432 D432 C8765", // South (0 HCP)
            "2C", "Pass"            // Or 2D negative
        ).SetName("StrongOpening_2C_Balanced25");

        // B. Strong 2C (Unbalanced, Game Force - 9 Playing Tricks)
        // S: AKQJxxxx H: AK D: Kx C: x
        yield return new TestCaseData(
            "SAKQJ982 HAK DK2 C4",  // North (Strong playing strength)
            "S543 H432 D432 C8765",
            "2C", "Pass"
        ).SetName("StrongOpening_2C_Unbalanced");

        // C. Small Slam Logic (Combined ~33 HCP)
        yield return new TestCaseData(
            "SAKQ2 HKQJ4 DA2 CJ32", // North (19 HCP)
            "SJ43 HA52 DKQJ5 Ckq4", // South (15 HCP) -> Total 34
            "1C", "6NT"             // Simplified expected end state
        ).SetName("Slam_6NT_Combined34");
    }

    // ==============================================================================
    // 3. UNBALANCED / PREEMPTS
    // ==============================================================================

    public static IEnumerable<TestCaseData> UnbalancedScenarios()
    {
        // A. Weak 2H (6-10 HCP, 6 Hearts)
        yield return new TestCaseData(
            "S42 HKJ9872 D87 CQJ3",  // North (8 HCP, 6 Hearts)
            "SAK5 HA4 DAKQ2 C982",   // South (Strong Responder)
            "2H", "4H"               // Responder raises to game
        ).SetName("WeakTwo_2H_RaiseToGame");

        // B. Preempt 3S (7 Card Suit, Weak)
        yield return new TestCaseData(
            "SKQJ9872 H43 D82 C54",  // North (6 HCP, 7 Spades)
            "SA4 HA2 DKQJ C9876",    // South
            "3S", "4S"
        ).SetName("Preempt_3S");

        // C. Extreme Distribution (8+ card suit) -> 4H Opening?
        yield return new TestCaseData(
            "S2 HKQJ98762 DA2 C43",  // North (10 HCP, 8 Hearts)
            "SAK43 H5 D8765 CAKQ",   // South
            "4H", "Pass"             // Opener starts high
        ).SetName("Preempt_4H_8CardSuit");
    }

    // ==============================================================================
    // 4. 1NT RESPONSES (Transfers & Stayman)
    // ==============================================================================

    public static IEnumerable<TestCaseData> NoTrumpScenarios()
    {
        // A. Transfer to Hearts (1NT - 2D - 2H)
        // North: 1NT (12-14)
        // South: 5+ Hearts, Any strength (0+ HCP)
        yield return new TestCaseData(
            "SKJ42 HQ53 DAJ2 CQ84",  // North (13 HCP, Balanced)
            "S87 HKJ982 D8765 C32",  // South (4 HCP, 5 Hearts)
            "1NT", "2D"              // Expect 2D transfer
        ).SetName("1NT_TransferToHearts_Weak");

        // B. Transfer to Spades (1NT - 2H - 2S)
        yield return new TestCaseData(
            "SAQ2 HKJ3 D9872 CAK3",  // North (14 HCP, Balanced)
            "SQT9872 H43 D432 C54",  // South (2 HCP, 6 Spades)
            "1NT", "2H"
        ).SetName("1NT_TransferToSpades_Weak");

        // C. Stayman (1NT - 2C) -> Response 2D (No Major)
        // South needs 11+ HCP and a 4-card major to ask
        yield return new TestCaseData(
            "SKJ2 HQJ2 DAQ32 C432",  // North (13 HCP, No 4-card major)
            "SA876 HK982 DK54 C87",  // South (11 HCP, 4S 4H)
            "1NT", "2C"              // Expect 2C
            // Follow up check: North should bid 2D
        ).SetName("Stayman_Response_2D");

        // D. Stayman -> Response 2H (4 Hearts)
        yield return new TestCaseData(
            "SK42 HKQJ4 DA32 C432",  // North (13 HCP, 4 Hearts)
            "SA876 H9832 DK5 C876",  // South (9 HCP? Stayman usually 11+. Let's give 11)
            // Adjusted South: SA876 HK982 DKJ5 C8 (11 HCP)
            "1NT", "2C"
        ).SetName("Stayman_Response_2H");

        // E. Stayman -> Response 2S (4 Spades, No Hearts)
        yield return new TestCaseData(
            "SKQJ4 H432 DA32 C432",  // North (13 HCP, 4 Spades)
            "SA876 HK982 DK5 C876",
            "1NT", "2C"
        ).SetName("Stayman_Response_2S");
        
        // F. Stayman -> Both Majors (Prioritize Hearts)
        yield return new TestCaseData(
            "SKQJ4 HKQJ4 DA2 C432",  // North (14 HCP, 4S 4H)
            "SA876 H9832 DK5 C876",
            "1NT", "2C"
            // Expected Reply: 2H (Standard Acol bids Hearts first)
        ).SetName("Stayman_Response_BothMajors");
    }
}