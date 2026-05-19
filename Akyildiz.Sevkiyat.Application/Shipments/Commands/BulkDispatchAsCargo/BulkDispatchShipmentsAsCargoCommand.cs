using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkDispatchAsCargo
{
    public record BulkDispatchShipmentsAsCargoCommand : IRequest<BulkDispatchAsCargoResult>, IRequireRoles
    {
        public List<int> ShipmentIds { get; init; } = new();
        public CargoProvider CargoProvider { get; init; }
        public string? CargoTrackingNumber { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public record BulkDispatchAsCargoResult(int SuccessCount, List<string> Errors);

    public class BulkDispatchShipmentsAsCargoCommandValidator : AbstractValidator<BulkDispatchShipmentsAsCargoCommand>
    {
        public BulkDispatchShipmentsAsCargoCommandValidator()
        {
            RuleFor(x => x.ShipmentIds).NotEmpty().Must(ids => ids.Count <= 200);
            RuleFor(x => x.CargoProvider).IsInEnum();
        }
    }

    public class BulkDispatchShipmentsAsCargoCommandHandler
        : IRequestHandler<BulkDispatchShipmentsAsCargoCommand, BulkDispatchAsCargoResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public BulkDispatchShipmentsAsCargoCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<BulkDispatchAsCargoResult> Handle(
            BulkDispatchShipmentsAsCargoCommand request,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            var shipments = await _context.Shipments
                .Where(s => request.ShipmentIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            int successCount = 0;

            foreach (var shipment in shipments)
            {
                try
                {
                    if (shipment.Status != ShipmentStatus.ReadyForDispatch)
                    {
                        errors.Add($"#{shipment.Id}: Sevkiyat 'Sevke Hazır' durumunda değil (mevcut: {shipment.Status}).");
                        continue;
                    }

                    shipment.SetCargoDispatch(request.CargoProvider, request.CargoTrackingNumber);
                    shipment.ChangeStatus(ShipmentStatus.Dispatched, _currentUserService.UserId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"#{shipment.Id}: {ex.Message}");
                }
            }

            var foundIds = shipments.Select(s => s.Id).ToHashSet();
            foreach (var id in request.ShipmentIds.Where(id => !foundIds.Contains(id)))
                errors.Add($"#{id}: Sevkiyat bulunamadı.");

            if (successCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return new BulkDispatchAsCargoResult(successCount, errors);
        }
    }
}
