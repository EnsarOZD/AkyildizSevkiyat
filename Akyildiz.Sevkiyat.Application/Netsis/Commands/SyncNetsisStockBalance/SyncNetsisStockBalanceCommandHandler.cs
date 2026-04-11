using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncNetsisStockBalance
{
    public class SyncNetsisStockBalanceCommandHandler
        : IRequestHandler<SyncNetsisStockBalanceCommand, SyncNetsisStockBalanceResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;
        private readonly ILogger<SyncNetsisStockBalanceCommandHandler> _logger;

        public SyncNetsisStockBalanceCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsisClient,
            ILogger<SyncNetsisStockBalanceCommandHandler> logger)
        {
            _context = context;
            _netsisClient = netsisClient;
            _logger = logger;
        }

        public async Task<SyncNetsisStockBalanceResult> Handle(
            SyncNetsisStockBalanceCommand request,
            CancellationToken cancellationToken)
        {
            // Netsis'ten bakiyeleri çek
            var balances = await _netsisClient.GetStockBalancesAsync(
                stokKodu: request.StokKodu,
                cancellationToken: cancellationToken);

            if (balances.Count == 0)
            {
                _logger.LogWarning("Netsis stok bakiye sorgusu boş döndü. StokKodu filtresi: {StokKodu}", request.StokKodu ?? "(tümü)");
                return new SyncNetsisStockBalanceResult { TotalReceived = 0 };
            }

            // Eşleşme: StockMaster.NetsisStockCode → Netsis StokKodu
            // NetsisStockCode null olan yerel stoklar eşleşmez; bu beklenen davranıştır.
            // Eşleşmeyen kodlar sonuç nesnesinde UnmatchedCodes listesinde raporlanır.
            var netsisCodesInResponse = balances.Select(b => b.StokKodu).ToHashSet();

            var localStocks = await _context.StockMasters
                .Where(s => s.NetsisStockCode != null && netsisCodesInResponse.Contains(s.NetsisStockCode))
                .ToListAsync(cancellationToken);

            var localByNetsisCode = localStocks
                .ToDictionary(s => s.NetsisStockCode!, StringComparer.OrdinalIgnoreCase);

            var unmatchedCodes = new List<string>();
            int matched = 0;

            foreach (var balance in balances)
            {
                if (!localByNetsisCode.TryGetValue(balance.StokKodu, out var stock))
                {
                    unmatchedCodes.Add(balance.StokKodu);
                    continue;
                }

                var previousQty = stock.OnHandQty;
                var newQty = balance.MevcutStok;

                stock.OverrideOnHand(newQty);

                // Fark varsa StockTransaction kaydı oluştur
                var diff = newQty - previousQty;
                if (diff != 0)
                {
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        StockMasterId = stock.Id,
                        Type          = StockTransactionType.ManualAdjust,
                        Qty           = diff,
                        Reference     = "NETSIS-SYNC",
                        Date          = DateTime.UtcNow,
                        Note          = $"Netsis stok senkronizasyonu — önceki: {previousQty}, yeni: {newQty}",
                    });
                }

                matched++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (unmatchedCodes.Count > 0)
                _logger.LogWarning(
                    "Netsis stok senkronizasyonu: {Unmatched} kod yerel stok tablosunda eşleşmedi. Kodlar: {Codes}",
                    unmatchedCodes.Count,
                    string.Join(", ", unmatchedCodes));

            _logger.LogInformation(
                "Netsis stok senkronizasyonu tamamlandı. Alınan: {Total}, Eşleşen: {Matched}, Eşleşmeyen: {Unmatched}",
                balances.Count, matched, unmatchedCodes.Count);

            return new SyncNetsisStockBalanceResult
            {
                TotalReceived  = balances.Count,
                Matched        = matched,
                Unmatched      = unmatchedCodes.Count,
                UnmatchedCodes = unmatchedCodes,
            };
        }
    }
}
