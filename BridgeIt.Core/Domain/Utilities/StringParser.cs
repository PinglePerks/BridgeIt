namespace BridgeIt.Core.Domain.Utilities;

public static class StringParser
{
    public static int ParseMinimum(string rangeString)
    {
        if (rangeString.StartsWith(">="))
        {
            return int.Parse(rangeString.Substring(2));
        }

        if (rangeString.Contains("-"))
        {
            var parts = rangeString.Split('-');
            return int.Parse(parts[0]);
        }
        return 0;
    }
}