using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.DeliverStop
{
    // ── Inputs ────────────────────────────────────────────────────────────────

    /// <summary>Bir sevkiyat satırının teslim sonucu. DeliveredQty = müşterinin kabul ettiği miktar.</summary>
    public record DeliverStopLineInput(
        int ShipmentLineId,
        decimal DeliveredQty,
        int? ReturnReason,
        string? ReturnReasonText);

    /// <summary>Önceki sevkiyatlardan gelen, mevcut teslimata bağlı olmayan harici iade.</summary>
    public record DeliverStopExternalReturnInput(
        int? StockMasterId,
        string? StockCodeFree,
        string? StockNameFree,
        decimal Qty,
        int ReturnReason,
        string? Note);

    public record DeliverStopCommand(
        int ProjectId,
        string DeliveryRecipient,
        string? DeliveryNote,
        List<string>? PhotosBase64,
        double? Latitude,
        double? Longitude,
        List<DeliverStopLineInput> Lines,
        List<DeliverStopExternalReturnInput>? ExternalReturns
    ) : IRequest<DeliverStopResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Driver" };
    }

    public record DeliverStopResult(int DeliveredShipments, int ReturnedShipments, int FloatingReturns);

    // ── Validator ─────────────────────────────────────────────────────────────

    public class DeliverStopCommandValidator : AbstractValidator<DeliverStopCommand>
    {
        public DeliverStopCommandValidator()
        {
            RuleFor(x => x.ProjectId).GreaterThan(0);
            RuleFor(x => x.DeliveryRecipient).NotEmpty().WithMessage("Teslim alan bilgisi zorunludur.").MaximumLength(200);
            RuleFor(x => x.PhotosBase64)
                .Must(p => p != null && p.Any(s => !string.IsNullOrWhiteSpace(s)))
                .WithMessage("En az bir teslim fotoğrafı zorunludur.");
            RuleForEach(x => x.Lines).ChildRules(l =>
            {
                l.RuleFor(i => i.DeliveredQty).GreaterThanOrEqualTo(0);
            });
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public class DeliverStopCommandHandler : IRequestHandler<DeliverStopCommand, DeliverStopResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IPhotoStorageService _photos;
        private readonly ZoneAutoCloseService _zoneAutoClose;

        public DeliverStopCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IPhotoStorageService photos,
            ZoneAutoCloseService zoneAutoClose)
        {
            _context = context;
            _currentUser = currentUser;
            _photos = photos;
            _zoneAutoClose = zoneAutoClose;
        }

        public async Task<DeliverStopResult> Handle(DeliverStopCommand request, CancellationToken cancellationToken)
        {
            // ── Bu durağın (proje) teslim edilecek sevkiyatlarını çöz ──
            var shipmentsQuery = _context.Shipments
                .Include(s => s.Lines)
                .Where(s => s.ProjectId == request.ProjectId
                         && (s.Status == ShipmentStatus.Dispatched || s.Status == ShipmentStatus.AssignedToVehicle));

            if (_currentUser.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");

                var openSessionId = await _context.DriverSessions
                    .Where(ds => ds.DriverId == driver.Id && ds.Status == DriverSessionStatus.Open)
                    .Select(ds => (Guid?)ds.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new DomainException("Teslim yapabilmek için önce QR okutarak sefer başlatmalısınız.");

                var manifestIds = await _context.DriverSessionShipments
                    .Where(m => m.DriverSessionId == openSessionId)
                    .Select(m => m.ShipmentId)
                    .ToListAsync(cancellationToken);

                // Manifest doluysa (yeni seferler) yalnızca manifestteki sevkiyatlar teslim edilebilir.
                if (manifestIds.Count > 0)
                    shipmentsQuery = shipmentsQuery.Where(s => manifestIds.Contains(s.Id));
                else
                    // Eski/boş manifestli sefer (geçiş): zone/atama bazlı sahiplik.
                    shipmentsQuery = shipmentsQuery.Where(s =>
                        s.AssignedDriverId == driver.Id ||
                        (s.ZonePreparationId != null &&
                         _context.ZonePreparationDrivers.Any(zpd =>
                             zpd.ZonePreparationId == s.ZonePreparationId && zpd.DriverId == driver.Id)));
            }

            var shipments = await shipmentsQuery.ToListAsync(cancellationToken);
            if (shipments.Count == 0)
                throw new DomainException("Bu teslim noktasında teslim edilecek sevkiyat bulunamadı.");

            // Satır eşlemesi + gelen satır id'lerinin bu duraktaki sevkiyatlara ait olduğunu doğrula
            var lineMap = shipments.SelectMany(s => s.Lines).ToDictionary(l => l.Id);
            var inputByLineId = new Dictionary<int, DeliverStopLineInput>();
            foreach (var li in request.Lines)
            {
                if (!lineMap.ContainsKey(li.ShipmentLineId))
                    throw new DomainException($"Satır #{li.ShipmentLineId} bu teslim noktasına ait değil.");
                inputByLineId[li.ShipmentLineId] = li;
            }

            // Stok kartlarını topla
            var stockIds = lineMap.Values
                .Where(l => l.StockMasterId.HasValue)
                .Select(l => l.StockMasterId!.Value)
                .Distinct()
                .ToList();
            var stockMap = stockIds.Count > 0
                ? (await _context.StockMasters.Where(s => stockIds.Contains(s.Id)).ToListAsync(cancellationToken))
                    .ToDictionary(s => s.Id)
                : new Dictionary<int, StockMaster>();

            var now = DateTime.UtcNow;
            var savedPhotoPaths = new List<string>();
            int deliveredCount = 0, returnedCount = 0;
            var affectedZoneIds = new HashSet<int>();

            // ── Her sevkiyat için teslim/iade uygula ──
            foreach (var shipment in shipments)
            {
                // Satır bazında iade (yüklenen - teslim) uygula
                foreach (var line in shipment.Lines)
                {
                    var loaded = line.DeliveredQty > 0 ? line.DeliveredQty : line.OrderedQty;

                    // Girdi yoksa: tamamı teslim sayılır (iade yok).
                    decimal accepted = inputByLineId.TryGetValue(line.Id, out var inp) ? inp.DeliveredQty : loaded;

                    if (accepted > loaded)
                        throw new DomainException(
                            $"{line.StockName}: teslim miktarı ({accepted}) yüklenen miktarı ({loaded}) aşamaz.");

                    var returnedQty = loaded - accepted;
                    if (returnedQty > 0)
                    {
                        if (inp is null || !inp.ReturnReason.HasValue)
                            throw new DomainException(
                                $"{line.StockName}: eksik/iade edilen ürün için sebep zorunludur.");

                        var reason = (ReturnReason)inp.ReturnReason.Value;
                        if (reason == ReturnReason.Other && string.IsNullOrWhiteSpace(inp.ReturnReasonText))
                            throw new DomainException($"{line.StockName}: 'Diğer' sebebi için açıklama giriniz.");

                        line.RecordReturn((line.ReturnedQty ?? 0) + returnedQty, reason);

                        // İade edilen ürün araçtan inmediği için OnHand'den DÜŞÜLMEZ; ancak depo
                        // atamasında bu miktar rezerve edilmişti — teslim edilen kısım aşağıda
                        // Deduct ile rezervden düşülür, iade edilen kısmın rezervasyonu burada
                        // serbest bırakılır (eski "teslim+iade" akışıyla paritede; aksi halde
                        // AvailableQty kalıcı olarak eksik görünürdü).
                        if (line.StockMasterId.HasValue
                            && stockMap.TryGetValue(line.StockMasterId.Value, out var rStock))
                        {
                            rStock.ReleaseReservation(returnedQty);
                            _context.StockTransactions.Add(new StockTransaction
                            {
                                StockMasterId = rStock.Id,
                                Type          = StockTransactionType.ReleaseReserve,
                                Qty           = -returnedQty,
                                Reference     = $"SHP-{shipment.Id}",
                                Date          = now,
                                Note          = $"Sevkiyat #{shipment.Id} teslimde iade — rezervasyon serbest"
                            });
                        }
                    }
                }

                // Tüm satırlar net 0 teslim → depoya iade; aksi halde teslim edildi.
                bool nothingDelivered = shipment.Lines.All(l => l.NetDeliveredQty <= 0);

                if (nothingDelivered)
                {
                    shipment.RecordVehicleReturn(now, request.DeliveryNote);
                    shipment.ChangeStatus(ShipmentStatus.ReturnedToWarehouse, _currentUser.UserId,
                        "Teslim noktasında tüm kalemler iade edildi.");
                    returnedCount++;
                }
                else
                {
                    shipment.RecordDelivery(
                        now, request.DeliveryRecipient, request.DeliveryNote,
                        null, _currentUser.UserId, _currentUser.Role?.ToString(), null,
                        null, request.Latitude, request.Longitude);

                    // Teslim fotoğrafları — her sevkiyata bağlanır (irsaliye başına kanıt)
                    var photos = request.PhotosBase64?.Where(p => !string.IsNullOrWhiteSpace(p)).Take(5).ToList()
                                 ?? new List<string>();
                    for (int i = 0; i < photos.Count; i++)
                    {
                        var path = await _photos.SaveDeliveryPhotoAsync(
                            photos[i], shipment.Id, shipment.IrsaliyeNo, i + 1, cancellationToken);
                        savedPhotoPaths.Add(path);
                        _context.ShipmentDeliveryPhotos.Add(new ShipmentDeliveryPhoto
                        {
                            ShipmentId = shipment.Id,
                            PhotoPath  = path,
                            PhotoIndex = i + 1,
                            TakenAt    = now,
                        });
                    }

                    shipment.ChangeStatus(ShipmentStatus.Delivered, _currentUser.UserId);

                    // Stok çıkışı — yalnızca net teslim edilen (kabul edilen) miktar
                    foreach (var line in shipment.Lines.Where(l => l.StockMasterId.HasValue))
                    {
                        var actualQty = line.NetDeliveredQty;
                        if (actualQty <= 0) continue;
                        if (!stockMap.TryGetValue(line.StockMasterId!.Value, out var stock)) continue;

                        stock.Deduct(actualQty);
                        _context.StockTransactions.Add(new StockTransaction
                        {
                            StockMasterId = stock.Id,
                            Type          = StockTransactionType.ShipmentOut,
                            Qty           = -actualQty,
                            Reference     = $"SHP-{shipment.Id}",
                            Date          = now,
                            Note          = $"Sevkiyat #{shipment.Id} teslim edildi (durak teslimi)"
                        });
                    }
                    deliveredCount++;
                }

                if (shipment.ZonePreparationId.HasValue)
                    affectedZoneIds.Add(shipment.ZonePreparationId.Value);
            }

            // ── Harici iadeler → FloatingReturn ──
            int floatingCount = 0;
            foreach (var er in request.ExternalReturns ?? new List<DeliverStopExternalReturnInput>())
            {
                if (er.Qty <= 0) continue;
                if (er.StockMasterId is null && string.IsNullOrWhiteSpace(er.StockCodeFree) && string.IsNullOrWhiteSpace(er.StockNameFree))
                    throw new DomainException("Harici iade için ürün (kart veya ad) belirtilmelidir.");

                _context.FloatingReturns.Add(new FloatingReturn
                {
                    ReturnDate      = now,
                    StockMasterId   = er.StockMasterId,
                    StockCodeFree   = er.StockCodeFree,
                    StockNameFree   = er.StockNameFree,
                    Qty             = er.Qty,
                    ReturnReason    = (ReturnReason)er.ReturnReason,
                    Note            = er.Note,
                    Status          = FloatingReturnStatus.Pending,
                    CreatedByUserId = _currentUser.UserId,
                    CreatedAt       = now,
                });
                floatingCount++;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var path in savedPhotoPaths)
                    await _photos.DeleteAsync(path);
                throw new ConflictException(
                    "Kayıt sırasında çakışma oluştu (aynı anda başka bir işlem). Lütfen tekrar deneyin.");
            }
            catch
            {
                foreach (var path in savedPhotoPaths)
                    await _photos.DeleteAsync(path);
                throw;
            }

            // Tüm sevkiyatlar final duruma geçtiyse etkilenen zone'ları kapat
            foreach (var zoneId in affectedZoneIds)
                await _zoneAutoClose.TryAutoCloseAsync(zoneId, cancellationToken);
            if (affectedZoneIds.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new DeliverStopResult(deliveredCount, returnedCount, floatingCount);
        }
    }
}
