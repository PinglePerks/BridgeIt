using BridgeIt.Core.Domain.Primatives;
using BridgeIt.TestHarness.DealerIntegrationTests;
using NUnit.Framework;

namespace BridgeIt.TestHarness.RealGameTests;

public class RealGameIntegrationTestCases
{
    public static IEnumerable<TestCaseData> NonGameTestCase()
    {

        yield return new TestCaseData(
            "[North: AKQ A62 K83 7642]\n[East: 974 K AQJ6542 QT]\n[South: T865 Q8754 T7 J8]\n[West: J32 JT93 9 AK953]",
            Seat.East,
            new List<string>
            {
                "1D",
                "Pass",
                "1H",
                "1NT",
                "2D"
            }
        ).SetName("Non-game hand");
    }


}
