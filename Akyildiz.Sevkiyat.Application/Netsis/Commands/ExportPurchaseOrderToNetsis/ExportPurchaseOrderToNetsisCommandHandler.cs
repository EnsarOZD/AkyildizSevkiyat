using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.ExportPurchaseOrderToNetsis
{
    public class ExportPurchaseOrderToNetsisCommandHandler : IRequestHandler<ExportPurchaseOrderToNetsisCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;

        public ExportPurchaseOrderToNetsisCommandHandler(IApplicationDbContext context, INetsisClient netsisClient)
        {
            _context = context;
            _netsisClient = netsisClient;
        }

        public async Task<string> Handle(ExportPurchaseOrderToNetsisCommand request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Lines)
                    .ThenInclude(l => l.StockMaster)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken)
                ?? throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);

            if (po.Status != PurchaseOrderStatus.Approved)
                throw new DomainException(
                    $"Satınalma siparişi Netsis'e aktarılabilir durumda değil. Mevcut durum: {po.Status}. " +
                    $"Gerekli durum: Approved.");

            if (po.NetsisTransferredAt.HasValue)
                throw new ConflictException(
                    $"Satınalma siparişi #{po.OrderNumber} zaten Netsis'e aktarılmış " +
                    $"({po.NetsisTransferredAt:dd.MM.yyyy HH:mm}).");

            if (string.IsNullOrWhiteSpace(po.Supplier.SupplierCode))
                throw new DomainException(
                    $"Tedarikçi '{po.Supplier.Name}' için Netsis Tedarikçi Kodu tanımlanmamış. " +
                    "Lütfen tedarikçi kaydına SupplierCode ekleyin.");

            var missingNetsisCode = po.Lines
                .Where(l => string.IsNullOrWhiteSpace(l.StockMaster.NetsisStockCode))
                .Select(l => l.StockMaster.StockName)
                .ToList();

            if (missingNetsisCode.Any())
                throw new DomainException(
                    $"Aşağıdaki ürünlerin Netsis Stok Kodu tanımlanmamış: {string.Join(", ", missingNetsisCode)}");

            var poRequest = BuildPoRequest(po);

            var result = await _netsisClient.CreateSatinalmaSiparisAsync(poRequest, cancellationToken);

            if (!result.Basarili)
                throw new DomainException(
                    $"Netsis satınalma sipariş aktarımı başarısız: {result.Mesaj ?? "Bilinmeyen hata"}");

            po.ExternalRef = result.NetsisPONo ?? po.OrderNumber;
            po.NetsisTransferredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return po.ExternalRef;
        }

        private static NetsisPoRequest BuildPoRequest(Domain.Entities.PurchaseOrder po)
        {
            var lines = po.Lines
                .Select((l, idx) => new NetsisPoLine
                {
                    SatirNo  = idx + 1,
                    StokKodu = l.StockMaster.NetsisStockCode!,
                    Miktar   = l.OrderedQty,
                    Birim    = l.Unit.ToString(),
                })
                .ToList();

            return new NetsisPoRequest
            {
                BelgeNo       = po.OrderNumber,
                TedarikciKodu = po.Supplier.SupplierCode!,
                SiparisDate   = po.OrderDate,
                TeslimTarihi  = po.ExpectedDeliveryDate,
                Aciklama      = po.Note,
                Satirlar      = lines,
            };
        }
    }
}
