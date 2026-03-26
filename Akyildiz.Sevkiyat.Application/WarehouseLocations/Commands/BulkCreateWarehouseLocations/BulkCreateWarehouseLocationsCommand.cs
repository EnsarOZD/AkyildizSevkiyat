using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.BulkCreateWarehouseLocations
{
    /// <summary>
    /// Bir koridor+taraf kombinasyonu için modül × kat kombinasyonlarını toplu oluşturur.
    /// Örn: KoridorNo=1, Taraf="K", Modül 1-20, Kat 1-5  → 100 lokasyon
    /// Kod formatı: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}  →  "1K-001-03"
    /// </summary>
    public record BulkCreateWarehouseLocationsCommand(
        int          KoridorNo,
        string       Taraf,        // "K" veya "G"
        int          ModulFrom,
        int          ModulTo,
        int          KatFrom,
        int          KatTo,
        LocationType LocationType
    ) : IRequest<BulkCreateResult>;

    public record BulkCreateResult(int Created, int Skipped);

    public class BulkCreateWarehouseLocationsCommandHandler
        : IRequestHandler<BulkCreateWarehouseLocationsCommand, BulkCreateResult>
    {
        private readonly IApplicationDbContext _context;

        public BulkCreateWarehouseLocationsCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BulkCreateResult> Handle(
            BulkCreateWarehouseLocationsCommand request,
            CancellationToken cancellationToken)
        {
            var taraf = request.Taraf.Trim().ToUpperInvariant();

            // Mevcut kodları önceden çek (çakışma kontrolü için)
            var existingCodes = await _context.WarehouseLocations
                .Where(l => l.KoridorNo == request.KoridorNo && l.Taraf == taraf)
                .Select(l => l.Code)
                .ToHashSetAsync(cancellationToken);

            var toAdd = new List<WarehouseLocation>();

            for (int modul = request.ModulFrom; modul <= request.ModulTo; modul++)
            for (int kat   = request.KatFrom;   kat   <= request.KatTo;   kat++)
            {
                var code = WarehouseLocation.BuildCode(request.KoridorNo, taraf, modul, kat);

                if (existingCodes.Contains(code)) continue;

                toAdd.Add(new WarehouseLocation
                {
                    Code         = code,
                    KoridorNo    = request.KoridorNo,
                    Taraf        = taraf,
                    ModulNo      = modul,
                    Kat          = kat,
                    LocationType = request.LocationType,
                    IsActive     = true,
                });
                existingCodes.Add(code);
            }

            if (toAdd.Count > 0)
            {
                _context.WarehouseLocations.AddRange(toAdd);
                await _context.SaveChangesAsync(cancellationToken);
            }

            int total = (request.ModulTo - request.ModulFrom + 1)
                      * (request.KatTo   - request.KatFrom   + 1);

            return new BulkCreateResult(toAdd.Count, total - toAdd.Count);
        }
    }
}
