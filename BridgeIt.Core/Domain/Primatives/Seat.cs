namespace BridgeIt.Core.Domain.Primatives;

public enum Seat
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public static class SeatExtensions
{
    public static string ToString(this Seat seat)
        => seat switch
        {
            Seat.North => "N",
            Seat.East => "E",
            Seat.South => "S",
            Seat.West => "W",
            _ => throw new ArgumentException($"Invalid seat: '{seat}'")
        };
    
    public static Seat GetPartner(this Seat seat) 
        => (Seat)(((int)seat + 2) % 4);
    
    public static Seat GetNextSeat(this Seat seat) 
        => (Seat)(((int)seat + 1) % 4);
}

