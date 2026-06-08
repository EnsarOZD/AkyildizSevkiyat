using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    public record ClothingPickLineDto(int ShipmentLineId, decimal DeliveredQty, string? DifferenceReason, string? Note);

    /// <summary>
    /// Kıyafet hazırlığını tamamlar: toplanan miktarları kaydeder (eksikte sebep zorunlu),
    /// hazırlayan + koli sayısını set eder ve Picking → ReadyForDispatch'e geçirir.
    /// </summary>
    public record CompleteClothingPreparationCommand(
        int ShipmentId, string? KoliCount, List<ClothingPickLineDto> Lines) : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class CompleteClothingPreparationCommandHandler : IRequestHandler<CompleteClothingPreparationCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CompleteClothingPreparationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(CompleteClothingPreparationCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            // Toplanan miktarları uygula (SetDeliveredQty eksik/fark durumunda sebep zorunlu kılar)
            var lineMap = shipment.Lines.ToDictionary(l => l.Id);
            foreach (var dto in request.Lines)
            {
                if (!lineMap.TryGetValue(dto.ShipmentLineId, out var line)) continue;
                line.SetDeliveredQty(dto.DeliveredQty, dto.DifferenceReason, dto.Note);
            }

            // Hazırlayan + koli + durum geçişi (domain guard: Clothing && Picking)
            shipment.CompleteClothingPreparation(
                _currentUser.FullName ?? _currentUser.Email ?? "Bilinmiyor",
                string.IsNullOrWhiteSpace(request.KoliCount) ? null : request.KoliCount.Trim(),
                _currentUser.UserId);

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
