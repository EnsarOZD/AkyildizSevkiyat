using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportClothingShipmentToNetsis
{
    /// <summary>
    /// Kıyafet operasyonu: Sevkiyatı Netsis'e Müşteri Siparişi olarak aktarır,
    /// ardından sevkiyatı doğrudan Delivered durumuna taşır.
    /// Sadece Created durumundaki Clothing sevkiyatlara uygulanabilir.
    /// </summary>
    public record ExportClothingShipmentToNetsisCommand(int ShipmentId) : IRequest<ExportClothingShipmentToNetsisResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Driver" };
    }

    public record ExportClothingShipmentToNetsisResult(
        string NetsisOrderNo,
        string? IrsaliyeNo,
        List<string> Warnings);
}
