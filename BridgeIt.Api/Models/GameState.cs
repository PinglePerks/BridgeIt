namespace BridgeIt.Api.Models;

public class GameState
{
    public string GameId { get; set; } = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
    public string Player1Id { get; set; }
    public string Player2Id { get; set; }
    
    // Game Data
    public List<string> Player1Hand { get; set; } = new();
    public List<string> Player2Hand { get; set; } = new();
    
    // Whose turn is it? (store the ConnectionId here)
    public string CurrentTurnPlayerId { get; set; } 
    
    public string StatusMessage { get; set; } = "Waiting for players...";
    
    // Helper to check if game is full
    public bool IsFull => !string.IsNullOrEmpty(Player1Id) && !string.IsNullOrEmpty(Player2Id);
}