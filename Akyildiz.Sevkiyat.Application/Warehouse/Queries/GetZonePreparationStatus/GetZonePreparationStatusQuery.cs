using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetZonePreparationStatus
{
    public record ZonePreparationDto(
        int Id,
        int ZoneId,
        string ZoneName,
        DateTime DeliveryDate,
        string Status,
        int StatusId,
        List<ZonePreparationProjectDto> Projects
    );

    public record ZonePreparationProjectDto(
        int Id,
        int ProjectId,
        string ProjectCode,
        string ProjectName,
        bool IsMicroReady,
        DateTime? MicroReadyAt
    );

    public record GetZonePreparationStatusQuery(int ZoneId, DateTime DeliveryDate) : IRequest<ZonePreparationDto>;

    public class GetZonePreparationStatusQueryHandler : IRequestHandler<GetZonePreparationStatusQuery, ZonePreparationDto>
    {
        private readonly IApplicationDbContext _context;

        public GetZonePreparationStatusQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ZonePreparationDto> Handle(GetZonePreparationStatusQuery request, CancellationToken cancellationToken)
        {
            var prep = await _context.ZonePreparations
                .Include(z => z.Zone)
                .Include(z => z.Projects)
                    .ThenInclude(p => p.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.ZoneId == request.ZoneId && z.DeliveryDate.Date == request.DeliveryDate.Date, cancellationToken);

            if (prep == null)
            {
                var zone = await _context.Zones.AsNoTracking()
                    .FirstOrDefaultAsync(z => z.Id == request.ZoneId, cancellationToken);
                
                return new ZonePreparationDto(
                    0, request.ZoneId, zone?.Name ?? "Unknown", request.DeliveryDate.Date, 
                    ZonePreparationStatus.Draft.ToString(), (int)ZonePreparationStatus.Draft, new List<ZonePreparationProjectDto>()
                );
            }

            // Return DTO
            return new ZonePreparationDto(
                prep.Id,
                prep.ZoneId,
                prep.Zone?.Name ?? "Unknown", 
                prep.DeliveryDate,
                prep.Status.ToString(),
                (int)prep.Status,
                prep.Projects.Select(p => new ZonePreparationProjectDto(
                    p.Id,
                    p.ProjectId,
                    p.Project?.Code ?? "N/A",
                    p.Project?.Name ?? "Unknown",
                    p.IsMicroReady,
                    p.MicroReadyAt
                )).ToList()
            );
        }
    }
}
