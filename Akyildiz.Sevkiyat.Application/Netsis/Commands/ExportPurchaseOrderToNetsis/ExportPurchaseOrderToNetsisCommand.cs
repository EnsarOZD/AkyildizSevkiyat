using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportPurchaseOrderToNetsis
{
    /// <summary>
    /// Belirtilen satınalma siparişini Netsis'e aktarır.
    /// Başarı durumunda PurchaseOrder.ExternalRef ve NetsisTransferredAt güncellenir.
    /// </summary>
    public record ExportPurchaseOrderToNetsisCommand(Guid PurchaseOrderId) : IRequest<string>;
    // Returns: Netsis tarafından atanan PO numarası
}
