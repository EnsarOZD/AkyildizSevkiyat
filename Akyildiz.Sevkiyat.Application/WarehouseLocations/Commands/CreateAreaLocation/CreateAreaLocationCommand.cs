using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreateAreaLocation
{
    /// <summary>
    /// Özel alan lokasyonlarını toplu oluşturur.
    /// Desteklenen tipler: FloorStack (zemin istif), Receiving (mal kabul), Returns (iade).
    /// Kod formatı: {PREFIX}-{001} — örn. MAL-001, IAD-002, DOK-003
    /// KoridorNo/Taraf/ModulNo/Kat bu tip lokasyonlar için anlamsızdır; sabit 0/"-"/0/0 atanır.
    /// </summary>
    public record CreateAreaLocationCommand(
        LocationType LocationType,
        string       Alan,
        string       Prefix,
        int          Count,
        string?      Description = null
    ) : IRequest<CreateAreaLocationResult>;

    public record CreateAreaLocationResult(int Created, int Skipped);

    public class CreateAreaLocationCommandHandler
        : IRequestHandler<CreateAreaLocationCommand, CreateAreaLocationResult>
    {
        private static readonly HashSet<LocationType> AllowedTypes =
        [
            LocationType.FloorStack,
            LocationType.Receiving,
            LocationType.Returns,
        ];

        private readonly IApplicationDbContext _context;

        public CreateAreaLocationCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<CreateAreaLocationResult> Handle(
            CreateAreaLocationCommand request,
            CancellationToken cancellationToken)
        {
            if (!AllowedTypes.Contains(request.LocationType))
                throw new DomainException($"CreateAreaLocation {request.LocationType} tipi için kullanılamaz.");

            if (string.IsNullOrWhiteSpace(request.Alan))
                throw new DomainException("Alan adı boş olamaz.");

            if (string.IsNullOrWhiteSpace(request.Prefix))
                throw new DomainException("Kod öneki (prefix) boş olamaz.");

            if (request.Count < 1 || request.Count > 100)
                throw new DomainException("Adet 1-100 arasında olmalıdır.");

            var prefix = request.Prefix.Trim().ToUpperInvariant();

            var existingCodes = await _context.WarehouseLocations
                .Where(l => l.Code.StartsWith(prefix + "-"))
                .Select(l => l.Code)
                .ToHashSetAsync(cancellationToken);

            var toAdd   = new List<WarehouseLocation>();
            int skipped = 0;
            int maxNo   = request.Count + existingCodes.Count + 100;

            for (int no = 1; no <= maxNo && toAdd.Count < request.Count; no++)
            {
                var code = WarehouseLocation.BuildAreaCode(prefix, no);
                if (existingCodes.Contains(code)) { skipped++; continue; }

                toAdd.Add(new WarehouseLocation
                {
                    Code         = code,
                    KoridorNo    = 0,
                    Taraf        = "-",
                    ModulNo      = 0,
                    Kat          = 0,
                    LocationType = request.LocationType,
                    Alan         = request.Alan.Trim(),
                    Description  = request.Description?.Trim(),
                    IsActive     = true,
                });
                existingCodes.Add(code);
            }

            if (toAdd.Count > 0)
            {
                _context.WarehouseLocations.AddRange(toAdd);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CreateAreaLocationResult(toAdd.Count, skipped);
        }
    }
}
