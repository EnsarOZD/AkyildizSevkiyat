using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking.Shortages
{
    /// <summary>
    /// "Gönder": seçilen eksik kayıtlardan tamamlama sevkiyat(lar)ı üretir. Aynı IssOrder/Project
    /// için TEK sevkiyat (Clothing, Created, yalnızca eksik satırlar). IssOrderId=null (mevcut
    /// sevkiyat o ISS siparişini tutuyor — unique index). ShortageRecord → DispatchRequested.
    /// </summary>
    public record DispatchShortagesCommand(List<int> ShortageIds) : IRequest<List<int>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class DispatchShortagesCommandHandler : IRequestHandler<DispatchShortagesCommand, List<int>>
    {
        private readonly IApplicationDbContext _context;
        public DispatchShortagesCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<List<int>> Handle(DispatchShortagesCommand request, CancellationToken ct)
        {
            if (request.ShortageIds is null || request.ShortageIds.Count == 0)
                throw new DomainException("En az bir eksik kaydı seçilmelidir.");

            var shortages = await _context.ShortageRecords
                .Where(r => request.ShortageIds.Contains(r.Id) && r.Status == ShortageStatus.Pending)
                .Include(r => r.Shipment).ThenInclude(s => s.Lines)
                .ToListAsync(ct);

            if (shortages.Count == 0)
                throw new DomainException("Gönderilebilecek (beklemede) eksik kaydı bulunamadı.");

            var createdShipmentIds = new List<int>();

            // Aynı kaynak ISS siparişi + proje → tek tamamlama sevkiyatı
            var groups = shortages.GroupBy(r => new { r.Shipment.IssOrderId, r.ProjectId });

            foreach (var g in groups)
            {
                var source = g.First().Shipment;

                var followup = new Shipment
                {
                    ProjectId = g.Key.ProjectId,
                    IssOrderId = null,                 // unique index — orijinal sevkiyat tutuyor
                    DeliveryDate = source.DeliveryDate,
                    TalepNo = source.TalepNo,
                    OperationType = OperationType.Clothing,
                    CreatedAt = DateTime.UtcNow,
                };

                foreach (var r in g)
                {
                    var origLine = source.Lines.FirstOrDefault(l => l.Id == r.ShipmentLineId);
                    var unit = origLine?.Unit ?? StockUnit.Adet;
                    followup.Lines.Add(ShipmentLine.Create(
                        null, r.StockMasterId, r.StockCode, r.StockName, unit, r.Qty));
                }

                _context.Shipments.Add(followup);
                await _context.SaveChangesAsync(ct); // followup.Id için

                foreach (var r in g)
                {
                    r.Status = ShortageStatus.DispatchRequested;
                    r.FollowupShipmentId = followup.Id;
                }

                createdShipmentIds.Add(followup.Id);
            }

            await _context.SaveChangesAsync(ct);
            return createdShipmentIds;
        }
    }
}
