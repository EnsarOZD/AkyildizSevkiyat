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

        /// <summary>
        /// Verilen ham ISS sipariş numaralarından hangilerinin Netsis'te mevcut olduğunu döndürür.
        /// ISS numarası → "000" + PadLeft(15) → TBLSIPUST.FATIRS_NO ile karşılaştırılır.
        /// Dönen tuple: (bulunan sipariş numaraları seti, varsa hata mesajı)
        /// </summary>
        Task<(IReadOnlySet<string> Found, string? Error)> CheckOrdersExistInNetsisAsync(
            IEnumerable<string> rawOrderNumbers,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Netsis stok kartlarından KDV oranlarını okur.
        /// Anahtar: NetsisStokKodu (büyük/küçük harf duyarsız), Değer: KDV yüzdesi (0, 1, 10, 20 vb.).
        /// Sorgu başarısız olursa boş sözlük döner — çağıran fallback uygulamalıdır.
        /// </summary>
        Task<IReadOnlyDictionary<string, decimal>> GetStockKdvRatesAsync(
            IEnumerable<string> netsisStokKodlari,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Daha önce Netsis'e aktarılmış sevkiyatların sipariş numaralarını kontrol eder.
        /// Missing  → Netsis'te hiç bulunamayan (silinmiş) → NetsisTransferredAt sıfırlanmalı.
        /// Foreign  → Netsis'te var ama EKACK1 ≠ bizim ShipmentId'miz (başka sistem aktarmış).
        ///            NetsisTransferredAt sıfırlanmamalı — sipariş var, "Aktar" butonu gizli kalmalı.
        /// Dönen tuple: (missing set, foreign set, varsa hata mesajı).
        /// NetsisOrderNumber değerleri ham veya formatlı olabilir; içinde FormatFatIrsNo uygulanır.
        /// </summary>
        Task<(IReadOnlySet<string> Missing, IReadOnlySet<string> Foreign, string? Error)> CheckShipmentOrdersMissingAsync(
            IEnumerable<(string NetsisOrderNo, string ShipmentId)> shipments,
            CancellationToken cancellationToken = default);
    }
}
