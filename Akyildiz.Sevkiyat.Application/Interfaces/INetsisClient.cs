using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface INetsisClient
    {
        /// <summary>
        /// Sevkiyatı Netsis'e "Müşteri Siparişi" olarak aktarır.
        /// </summary>
        Task<NetsisSiparisResult> CreateSiparisAsync(NetsisSiparisRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Satınalma siparişini Netsis'e aktarır.
        /// </summary>
        Task<NetsisPoResult> CreateSatinalmaSiparisAsync(NetsisPoRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Netsis'ten irsaliye bilgilerini okur (sipariş numarası veya tarih aralığıyla).
        /// </summary>
        Task<IReadOnlyList<NetsisIrsaliyeDto>> GetIrsaliyelerAsync(NetsisIrsaliyeQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Netsis'ten anlık stok bakiyelerini okur.
        /// </summary>
        Task<IReadOnlyList<NetsisStockBalanceDto>> GetStockBalancesAsync(
            string? stokKodu = null,
            string? depoKodu = null,
            CancellationToken cancellationToken = default);
    }
}
