using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.ResolveFloatingReturn
{
    public enum ResolveAction
    {
        MatchToShipment = 1,
        AddToStock = 2,
        WriteOff = 3
    }

    /// <summary>
    /// Bekleyen floating return'ü çözüme kavuşturur.
    /// MatchToShipment → LinkedShipmentId zorunlu
    /// AddToStock → stoğa giriş yapılır (StockMasterId zorunlu)
    /// WriteOff → hariç tutulur
    /// </summary>
    public record ResolveFloatingReturnCommand(
        int FloatingReturnId,
        ResolveAction Action,
        int? LinkedShipmentId = null,
        string? Note = null
    ) : IRequest<Unit>;
}
