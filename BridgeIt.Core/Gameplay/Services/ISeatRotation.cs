using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Services;

public interface ISeatRotationService
{
    Seat Next(Seat seat);
    Seat PartnerOf(Seat seat);
}