using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis
{
    /// <summary>
    /// Belirtilen sevkiyatı Netsis'e "Müşteri Siparişi" olarak aktarır.
    /// Başarı durumunda Shipment.NetsisTransferredAt ve IssOrder.NetsisOrderNumber güncellenir.
    /// </summary>
    public record ExportShipmentToNetsisCommand(int ShipmentId) : IRequest<string>;
    // Returns: Netsis tarafından atanan sipariş/belge numarası
}
