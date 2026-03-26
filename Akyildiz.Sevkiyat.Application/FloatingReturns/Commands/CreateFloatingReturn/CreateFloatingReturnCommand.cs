using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.CreateFloatingReturn
{
    /// <summary>
    /// Kaynağı bilinmeyen (hangi sevkiyata ait olduğu belirsiz) araç iadelerini kaydeder.
    /// </summary>
    public record CreateFloatingReturnCommand(
        DateTime ReturnDate,
        int? StockMasterId,
        string? StockCodeFree,
        string? StockNameFree,
        decimal Qty,
        ReturnReason ReturnReason,
        string? Note
    ) : IRequest<int>;
}
