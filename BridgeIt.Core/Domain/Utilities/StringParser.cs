// namespace BridgeIt.Core.Domain.Utilities;
//
// public static class StringParser
// {
//     public static (int, int) Parse(string rangeString)
//     {
//         var min = 0;
//         var max = 100;
//         
//         if (rangeString.Contains('-'))
//         {
//             var parts = rangeString.Split('-');
//             min = int.Parse(parts[0]);
//            max = int.Parse(parts[1]);
//         }
//         else if (rangeString.StartsWith(">="))
//         {
//             min = int.Parse(rangeString.Substring(2));
//            max = 40;
//         }
//         else if (rangeString.StartsWith("<="))
//         {
//            max = int.Parse(rangeString.Substring(2));
//             min = 0;
//         }
//         else
//         {
//             min = int.Parse(rangeString);
//            max = min;
//         }
//     }
//     
// }