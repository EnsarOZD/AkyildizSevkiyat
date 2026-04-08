using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportShipmentToNetsis
{
    /// <summary>
    /// Belirtilen sevkiyatı Netsis'e "Müşteri Siparişi" olarak aktarır.
    /// Başarı durumunda Shipment.NetsisTransferredAt ve IssOrder.NetsisOrderNumber güncellenir.
    /// </summary>
    public record ExportShipmentToNetsisCommand(int ShipmentId) : IRequest<ExportShipmentToNetsisResult>;

    /// <summary>
    /// Netsis aktarım sonucu. NetsisOrderNo başarı durumunda dolu, Warnings sıfır miktar olan kalemleri listeler.
    /// </summary>
    public record ExportShipmentToNetsisResult(string NetsisOrderNo, List<string> Warnings);
}
