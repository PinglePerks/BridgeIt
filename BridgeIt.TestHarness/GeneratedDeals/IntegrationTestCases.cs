using NUnit.Framework;

namespace BridgeIt.TestHarness.DealerIntegrationTests;

public class IntegrationTestCases
{
    public static IEnumerable<TestCaseData> TransferToSpadesScenarios()
    {

        yield return new TestCaseData(
            HandSpecifications.Strong2NTOpener,
            HandSpecifications.TransferToSpadesResponder,
            new List<string>
            {
                "2NT",
                "3H",
                "3S"
            }
        ).SetName("Strong2NT_TransferToSpades_2NT_3H_3S");
        
        yield return new TestCaseData(
            HandSpecifications.Open1NT, // North (13 HCP)
            HandSpecifications.TransferToSpadesResponder, // South (7 HCP, 3 Hearts)
            new List<string>
            {
                "1NT",
                "2H",
                "2S"
            }
        ).SetName("1NT_TransferToSpades_1NT_2H_2S");
    }
    
    public static IEnumerable<TestCaseData> TransferToHeartsScenarios()
    {

        yield return new TestCaseData(
            HandSpecifications.Strong2NTOpener,
            HandSpecifications.TransferToHeartsResponder,
            new List<string>
            {
                "2NT",
                "3D",
                "3H"
            }
        ).SetName("Strong2NT_TransferToHearts_2NT_3D_3H");
        
        yield return new TestCaseData(
            HandSpecifications.Open1NT, // North (13 HCP)
            HandSpecifications.TransferToHeartsResponder, // South (7 HCP, 3 Hearts)
            new List<string>
            {
                "1NT",
                "2D",
                "2H"
            }
        ).SetName("1NT_TransferToHearts_1NT_2D_2H");
    }
    public static IEnumerable<TestCaseData> InvitationalNtScenarios()
    {

        yield return new TestCaseData(
            HandSpecifications.Open1NT,
            HandSpecifications.Spades2Response,
            new List<string>
            {
                "1NT",
                "2S"
            }
        ).SetName("1NT_11_points_responder");
        
        yield return new TestCaseData(
            HandSpecifications.Open1NT, // North (13 HCP)
            HandSpecifications.NT2Response, // South (7 HCP, 3 Hearts)
            new List<string>
            {
                "1NT",
                "2NT",
            }
        ).SetName("1NT_TransferToHearts_1NT_2D_2H");
    }
    
    public static IEnumerable<TestCaseData> WeakPassScenarios()
    {

        yield return new TestCaseData(
            HandSpecifications.WeakPass,
            null,
            new List<string>
            {
                "Pass"
            }
        ).SetName("WeakPass_notopening");
        
    }
    public static IEnumerable<TestCaseData> MinRespondHand()
    {

        yield return new TestCaseData(
            HandSpecifications.Open1NT,
            HandSpecifications.BalancedOpener(0,6),
            new List<string>
            {
                "1NT",
                "Pass"
            }
        ).SetName("WeakPass_notopening");
        
    }
    
    public static IEnumerable<TestCaseData> AgreeAMajorFit()
    {

        yield return new TestCaseData(
            HandSpecifications.Hearts5Cards(15,19),
            HandSpecifications.Hearts5CardsLosers(9,10),
            new List<string>
            {
                "1H",
                "2H"
            }
        ).SetName("MajorFit_LimitRaise_1H_2H");
        
        yield return new TestCaseData(
            HandSpecifications.Hearts5Cards(15,19),
            HandSpecifications.Hearts5CardsLosers(8,8),
            new List<string>
            {
                "1H",
                "3H"
            }
        ).SetName("MajorFit_InvitationalRaise_1H_3H");
        
        yield return new TestCaseData(
            HandSpecifications.Hearts5Cards(15,19),
            HandSpecifications.Hearts5CardsLosers(5,7),
            new List<string>
            {
                "1H",
                "4H"
            }
        ).SetName("MajorFit_GameHand_1H_4H");
    }
    public static IEnumerable<TestCaseData> SlamHands()
    {

        yield return new TestCaseData(
            HandSpecifications.BalancedOpener(20,22),
            HandSpecifications.BalancedOpener(13,17),
            new List<string>
            {
                "2NT",
                "6NT"
            }
        ).SetName("Slam_Hand");
    }
    
}
