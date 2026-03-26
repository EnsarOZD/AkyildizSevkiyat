using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreateWarehouseLocation
{
    /// <summary>
    /// Tek bir lokasyon oluşturur.
    /// Kod otomatik hesaplanır: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}
    /// </summary>
    public record CreateWarehouseLocationCommand(
        int          KoridorNo,
        string       Taraf,       // "K" veya "G"
        int          ModulNo,
        int          Kat,
        LocationType LocationType,
        string?      Description,
        decimal?     MaxWeightKg,
        int?         MaxPallets
    ) : IRequest<int>;

    public class CreateWarehouseLocationCommandHandler : IRequestHandler<CreateWarehouseLocationCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateWarehouseLocationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateWarehouseLocationCommand request, CancellationToken cancellationToken)
        {
            var code = WarehouseLocation.BuildCode(request.KoridorNo, request.Taraf, request.ModulNo, request.Kat);

            var exists = await _context.WarehouseLocations
                .AnyAsync(l => l.Code == code, cancellationToken);

            if (exists)
                throw new ConflictException($"'{code}' kodlu lokasyon zaten mevcut.");

            var entity = new WarehouseLocation
            {
                Code         = code,
                KoridorNo    = request.KoridorNo,
                Taraf        = request.Taraf.Trim().ToUpperInvariant(),
                ModulNo      = request.ModulNo,
                Kat          = request.Kat,
                LocationType = request.LocationType,
                Description  = request.Description?.Trim(),
                MaxWeightKg  = request.MaxWeightKg,
                MaxPallets   = request.MaxPallets,
                IsActive     = true,
            };

            _context.WarehouseLocations.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
