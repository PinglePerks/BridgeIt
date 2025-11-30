using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Services;

public class ClockwiseSeatRotationService : ISeatRotationService
{
    public Seat Next(Seat seat) => (Seat)(((int)seat + 1) % 4);
    
    public Seat PartnerOf(Seat seat) =>
        seat switch
        {
            Seat.North => Seat.South,
            Seat.South => Seat.North,
            Seat.East  => Seat.West,
            Seat.West  => Seat.East,
            _ => throw new ArgumentOutOfRangeException(nameof(seat))
        };}