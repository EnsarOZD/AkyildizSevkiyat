using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncNetsisStockBalance
{
    /// <summary>
    /// Netsis'ten anlık stok bakiyelerini çeker ve StockMaster.OnHandQty'yi günceller.
    /// stokKodu null ise tüm stoklar senkronize edilir.
    /// </summary>
    public record SyncNetsisStockBalanceCommand(string? StokKodu = null) : IRequest<SyncNetsisStockBalanceResult>;

    public sealed class SyncNetsisStockBalanceResult
    {
        public int TotalReceived  { get; init; }
        public int Matched        { get; init; }
        public int Unmatched      { get; init; }
        public List<string> UnmatchedCodes { get; init; } = new();
    }
}
