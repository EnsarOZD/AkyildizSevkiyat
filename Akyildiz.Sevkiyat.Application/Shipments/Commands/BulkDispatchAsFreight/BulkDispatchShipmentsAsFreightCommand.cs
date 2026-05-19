using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.BulkDispatchAsFreight
{
    public record BulkDispatchShipmentsAsFreightCommand : IRequest<BulkDispatchAsFreightResult>, IRequireRoles
    {
        public List<int> ShipmentIds { get; init; } = new();
        public string CarrierName { get; init; } = string.Empty;
        public string? CarrierPlate { get; init; }
        public string? CarrierPhone { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public record BulkDispatchAsFreightResult(int SuccessCount, List<string> Errors);

    public class BulkDispatchShipmentsAsFreightCommandValidator : AbstractValidator<BulkDispatchShipmentsAsFreightCommand>
    {
        public BulkDispatchShipmentsAsFreightCommandValidator()
        {
            RuleFor(x => x.ShipmentIds).NotEmpty().Must(ids => ids.Count <= 200);
            RuleFor(x => x.CarrierName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CarrierPlate).MaximumLength(20).When(x => x.CarrierPlate != null);
            RuleFor(x => x.CarrierPhone).MaximumLength(30).When(x => x.CarrierPhone != null);
        }
    }

    public class BulkDispatchShipmentsAsFreightCommandHandler
        : IRequestHandler<BulkDispatchShipmentsAsFreightCommand, BulkDispatchAsFreightResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public BulkDispatchShipmentsAsFreightCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<BulkDispatchAsFreightResult> Handle(
            BulkDispatchShipmentsAsFreightCommand request,
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

                    shipment.SetFreightDispatch(request.CarrierName, request.CarrierPlate, request.CarrierPhone);
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

            return new BulkDispatchAsFreightResult(successCount, errors);
        }
    }
}
