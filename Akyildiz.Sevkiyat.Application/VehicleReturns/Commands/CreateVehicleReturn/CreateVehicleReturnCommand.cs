using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.VehicleReturns.Commands.CreateVehicleReturn
{
    public record CreateVehicleReturnLineInput(
        int? StockMasterId,
        string? StockCodeFree,
        string? StockNameFree,
        decimal Qty,
        string? Note
    );

    public record CreateVehicleReturnCommand(
        Guid DriverSessionId,
        DateTime? ReturnDate,
        string? Note,
        List<CreateVehicleReturnLineInput> Lines
    ) : IRequest<int>;

    public class CreateVehicleReturnCommandHandler : IRequestHandler<CreateVehicleReturnCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateVehicleReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(CreateVehicleReturnCommand request, CancellationToken cancellationToken)
        {
            var session = await _context.DriverSessions
                .FirstOrDefaultAsync(ds => ds.Id == request.DriverSessionId, cancellationToken);

            if (session == null)
                throw new NotFoundException("DriverSession", request.DriverSessionId);

            if (!request.Lines.Any())
                throw new DomainException("En az bir ürün satırı girilmelidir.");

            foreach (var line in request.Lines)
            {
                if (line.StockMasterId == null && string.IsNullOrWhiteSpace(line.StockNameFree))
                    throw new DomainException("Her satır için stok kartı veya ürün adı girilmelidir.");
                if (line.Qty <= 0)
                    throw new DomainException("Miktar sıfırdan büyük olmalıdır.");

                if (line.StockMasterId.HasValue)
                {
                    var exists = await _context.StockMasters.AnyAsync(s => s.Id == line.StockMasterId.Value, cancellationToken);
                    if (!exists)
                        throw new NotFoundException("StockMaster", line.StockMasterId.Value);
                }
            }

            var vehicleReturn = new VehicleReturn
            {
                DriverSessionId = request.DriverSessionId,
                ReturnDate = request.ReturnDate ?? DateTime.UtcNow,
                Note = request.Note,
                CreatedByUserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                Lines = request.Lines.Select(l => new VehicleReturnLine
                {
                    StockMasterId = l.StockMasterId,
                    StockCodeFree = l.StockCodeFree,
                    StockNameFree = l.StockNameFree,
                    Qty = l.Qty,
                    Note = l.Note
                }).ToList()
            };

            _context.VehicleReturns.Add(vehicleReturn);
            await _context.SaveChangesAsync(cancellationToken);

            return vehicleReturn.Id;
        }
    }
}
