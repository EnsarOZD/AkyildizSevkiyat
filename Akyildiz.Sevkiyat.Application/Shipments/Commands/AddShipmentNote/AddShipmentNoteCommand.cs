using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.AddShipmentNote
{
    /// <summary>
    /// Bir sevkiyata sonradan serbest not ekler — durumu DEĞİŞTİRMEZ.
    /// Teslim edilmiş / iade edilmiş (pasif) sevkiyatlara şoförün sonradan
    /// not ekleyebilmesi için kullanılır. Not, ShipmentHistory'ye yazılır
    /// (mevcut teslim notu ezilmez).
    /// </summary>
    public record AddShipmentNoteCommand(int ShipmentId, string Note) : IRequest;

    public class AddShipmentNoteCommandValidator : AbstractValidator<AddShipmentNoteCommand>
    {
        public AddShipmentNoteCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.Note)
                .NotEmpty().WithMessage("Not boş olamaz.")
                .MaximumLength(1000);
        }
    }

    public class AddShipmentNoteCommandHandler : IRequestHandler<AddShipmentNoteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AddShipmentNoteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(AddShipmentNoteCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            // Şoför yalnızca kendine atanmış sevkiyata not ekleyebilir
            if (_currentUserService.Role == UserRole.Driver)
            {
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken)
                    ?? throw new ForbiddenException("Kullanıcıya tanımlı bir şoför kaydı bulunamadı.");

                if (shipment.AssignedDriverId != driver.Id)
                    throw new ForbiddenException("Bu sevkiyat size atanmamış.");
            }

            shipment.Histories.Add(new ShipmentHistory
            {
                ShipmentId      = shipment.Id,
                OldStatus       = shipment.Status,
                NewStatus       = shipment.Status,
                ChangedByUserId = _currentUserService.UserId,
                ChangedAt       = DateTime.UtcNow,
                Description     = $"Not eklendi: {request.Note}"
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
