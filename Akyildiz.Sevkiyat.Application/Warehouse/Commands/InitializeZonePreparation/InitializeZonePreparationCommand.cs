using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Commands.InitializeZonePreparation
{
    public record InitializeZonePreparationCommand(int ZoneId, DateTime DeliveryDate) : IRequest<bool>;

    public class InitializeZonePreparationCommandHandler : IRequestHandler<InitializeZonePreparationCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public InitializeZonePreparationCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(InitializeZonePreparationCommand request, CancellationToken cancellationToken)
        {
            var date = request.DeliveryDate.Date;

            var existing = await _context.ZonePreparations
                .FirstOrDefaultAsync(z => z.ZoneId == request.ZoneId && z.DeliveryDate == date && z.BatchNo == 1, cancellationToken);

            if (existing != null)
            {
                return true;
            }

            var prep = new ZonePreparation
            {
                ZoneId = request.ZoneId,
                DeliveryDate = date,
                BatchNo = 1,
                Status = ZonePreparationStatus.Draft
            };

            _context.ZonePreparations.Add(prep);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrency Handling: Check if it's a unique constraint violation on the target index
                // Note: We'd typically check the DB provider's error code, but since we know the index, 
                // we'll verify if the record now exists.
                
                var conflict = await _context.ZonePreparations
                    .AnyAsync(z => z.ZoneId == request.ZoneId && z.DeliveryDate == date && z.BatchNo == 1, cancellationToken);

                if (conflict)
                {
                    // Swallowing expected unique-constraint collision
                    return true; 
                }

                // Rethrow unexpected exceptions
                throw;
            }

            return true;
        }
    }
}
