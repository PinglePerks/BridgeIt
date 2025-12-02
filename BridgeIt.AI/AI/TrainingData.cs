using BridgeIt.Analysis.Parsers;

namespace BridgeIt.Analysis.MachineLearning;

public class TrainingData
{
    public void GenerateTrainingData(string pbnFilePath, string outputCsvPath)
    {
        var parser = new PbnParser();
        var boards = parser.ParseFile(pbnFilePath);
    
        using var writer = new StreamWriter(outputCsvPath);
    
        // Write Header
        // Feature columns: C2, C3... SA
        // Label column: Bid
        var headers = Enumerable.Range(0, 52)
            .Select(i => HandVectorizer.GetCardNameFromIndex(i));
        writer.WriteLine(string.Join(",", headers) + ",Label");

        foreach (var board in boards)
        {
            // 1. Identify the dealer (Opening Bidder)
            var dealerSeat = board.Dealer;
            var openingHand = board.Hands[dealerSeat];
        
            // 2. Identify the Opening Bid
            // The first bid in the auction is made by the Dealer.
            if (board.ActualAuction.Count == 0) continue;
        
            var openingBid = board.ActualAuction[0];
        
            // Optional: Filter out "Pass" if you only want to train positive bids, 
            // or keep "Pass" to train the bot when NOT to bid.
        
            // 3. Vectorize
            var vector = HandVectorizer.Vectorize(openingHand);
        
            // 4. Write Row
            var vectorString = string.Join(",", vector);
            writer.WriteLine($"{vectorString},{openingBid}");
        }
    }
}