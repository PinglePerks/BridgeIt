using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Core.Gameplay.Output;

public interface IHandFormatter
{
    string FormatHand(Hand hand);
}