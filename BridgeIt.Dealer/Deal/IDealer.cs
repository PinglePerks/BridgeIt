using BridgeIt.Core.Domain.Primatives;

namespace BridgeIt.Dealer.Deal;

public interface IDealer
{
    Dictionary<Seat, Hand> GenerateRandomDeal();
}