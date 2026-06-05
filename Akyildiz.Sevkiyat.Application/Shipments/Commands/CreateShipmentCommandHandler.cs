using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands
{
    public class CreateShipmentCommandHandler
        : IRequestHandler<CreateShipmentCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsis;
        private readonly ILogger<CreateShipmentCommandHandler> _logger;

        public CreateShipmentCommandHandler(
            IApplicationDbContext context,
            INetsisClient netsis,
            ILogger<CreateShipmentCommandHandler> logger)
        {
            _context = context;
            _netsis  = netsis;
            _logger  = logger;
        }

        public async Task<int> Handle(
            CreateShipmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1) İlgili ISS siparişini satırları ile birlikte bul
            var order = await _context.IssOrders
                .Include(o => o.Project)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == request.IssOrderId, cancellationToken);

            if (order == null)
                throw new NotFoundException("ISS siparişi bulunamadı.");

            // Check if already transferred via flag
            if (order.IsTransferred)
                throw new ConflictException("Bu sipariş zaten bir sevkiyata dönüştürülmüş.");

            // Bir ISS siparişi için zaten bir sevkiyat varsa (iptal edilmiş olsa bile) yeni
            // sevkiyat oluşturulamaz. DB'deki unique index (IX_Shipments_IssOrderId) iptal
            // edilmiş sevkiyatları da kapsadığından, iptal edilenleri burada hariç tutmak
            // INSERT sırasında duplicate-key (2601) → 500 hatasına yol açıyordu. Bu kontrolü
            // statüden bağımsız yaparak temiz bir Conflict (409) mesajı döndürüyoruz.
            var existingShipment = await _context.Shipments
                .Where(s => s.IssOrderId == request.IssOrderId)
                .Select(s => new { s.NetsisTransferredAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingShipment != null)
            {
                var msg = existingShipment.NetsisTransferredAt != null
                    ? "Bu siparişin sevkiyatı daha önce Netsis'e aktarılmış. Yeni sevkiyat oluşturulamaz."
                    : "Bu sipariş zaten içeri aktarılmış (mevcut bir sevkiyatı var). Yeni sevkiyat oluşturulamaz.";
                throw new ConflictException(msg);
            }

            if (order.ImportStatus == ImportStatus.NeedsMapping)
                throw new DomainException("Stok eşleştirmesi tamamlanmadan sevkiyat oluşturulamaz.");

            if (!order.IsActive)
                throw new DomainException("Pasif sipariş sevkiyata dönüştürülemez.");

            if (!order.Lines.Any())
                throw new DomainException("Siparişin satırı bulunmuyor, sevkiyat oluşturulamaz.");

            // Netsis kontrolü: sipariş zaten Netsis'te varsa aktarım engellenir
            if (!string.IsNullOrWhiteSpace(order.ExternalOrderNumber))
            {
                try
                {
                    var (found, netsisError) = await _netsis.CheckOrdersExistInNetsisAsync(
                        new[] { order.ExternalOrderNumber }, cancellationToken);

                    if (found.Contains(order.ExternalOrderNumber))
                    {
                        order.IsTransferred = true;
                        await _context.SaveChangesAsync(cancellationToken);
                        throw new ConflictException(
                            $"Bu sipariş ({order.ExternalOrderNumber}) zaten Netsis'te mevcut. " +
                            "Yeniden aktarım yapılmayacak. Sipariş aktarıldı olarak işaretlendi.");
                    }

                    if (netsisError != null)
                        _logger.LogWarning(
                            "CreateShipment Netsis kontrolü başarısız [{OrderNo}]: {Error} — sevkiyat yine de oluşturuluyor.",
                            order.ExternalOrderNumber, netsisError);
                }
                catch (ConflictException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "CreateShipment Netsis kontrolü exception [{OrderNo}]: {Msg} — sevkiyat yine de oluşturuluyor.",
                        order.ExternalOrderNumber, ex.Message);
                }
            }

            // Load all relevant stock mappings (Mapped + Ignored)
            var stockCodes = order.Lines.Select(l => l.StockCode).Distinct().ToList();
            var allMappings = await _context.StockMappings
                .Where(m => stockCodes.Contains(m.ExternalStockCode) && m.ExternalSystem == "ISS-IP")
                .ToListAsync(cancellationToken);

            // Block creation if any line's stock code is explicitly ignored
            var ignoredCodes = allMappings
                .Where(m => m.MatchStatus == MatchStatus.Ignored)
                .Select(m => m.ExternalStockCode)
                .ToList();

            if (ignoredCodes.Any())
                throw new DomainException(
                    $"Bu sipariş yoksayılan stok kodları içeriyor ve sevkiyata alınamaz: {string.Join(", ", ignoredCodes)}. " +
                    "Lütfen eşleştirme ekranında bu stok kodlarını doğru bir yerel stokla eşleştirin.");

            var mappingLookup = allMappings
                .Where(m => m.MatchStatus == MatchStatus.Mapped && m.LocalStockId != null)
                .ToDictionary(m => m.ExternalStockCode, m => m.LocalStockId);

            // Operasyon tipini eşleşmiş stok kategorilerinden belirle.
            // Tüm eşleşmiş satırlar StockCategory.Kiyafet ise → Clothing, aksi halde → Catering.
            // Project.OperationType'a güvenilmez: aynı proje hem catering hem kıyafet siparişi alabilir.
            var mappedStockIds = mappingLookup.Values
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .ToList();

            var operationType = OperationType.Catering;
            if (mappedStockIds.Count > 0)
            {
                var categories = await _context.StockMasters
                    .Where(s => mappedStockIds.Contains(s.Id))
                    .Select(s => s.Category)
                    .ToListAsync(cancellationToken);

                if (categories.Count > 0 && categories.All(c => c == Akyildiz.Sevkiyat.Domain.Enums.StockCategory.Kiyafet))
                    operationType = OperationType.Clothing;
            }

            // 2) Yeni Shipment oluştur
            var shipment = new Shipment
            {
                ProjectId = order.ProjectId,
                DeliveryDate = order.DeliveryDate,
                IssOrderId = order.Id,
                CreatedAt = DateTime.UtcNow,
                TalepNo = order.TalepNo, // Transfer TalepNo if available
                OperationType = operationType
            };

            // 3) Her order line için bir shipment line ekle
            foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
            {
                var sLine = ShipmentLine.Create(
                    line.Id,
                    mappingLookup.TryGetValue(line.StockCode, out var sid) ? sid : null,
                    line.StockCode,
                    line.StockName,
                    line.Unit,
                    line.OrderedQty
                );

                shipment.Lines.Add(sLine);
            }

            // 4) Kaydet ve İşaretle
            _context.Shipments.Add(shipment);
            order.IsTransferred = true; // Mark as transferred
            
            await _context.SaveChangesAsync(cancellationToken);

            return shipment.Id;
        }
    }
}
