using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPreparation
{
    /// <summary>Seçilen kıyafet sevkiyatlarını depo hazırlığına alır (Created → Picking).</summary>
    public record StartClothingPreparationCommand(List<int> ShipmentIds) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class StartClothingPreparationCommandHandler : IRequestHandler<StartClothingPreparationCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public StartClothingPreparationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(StartClothingPreparationCommand request, CancellationToken cancellationToken)
        {
            if (request.ShipmentIds is null || request.ShipmentIds.Count == 0)
                throw new DomainException("En az bir sevkiyat seçilmelidir.");

            var shipments = await _context.Shipments
                .Where(s => request.ShipmentIds.Contains(s.Id)
                         && s.OperationType == OperationType.Clothing
                         && s.Status == ShipmentStatus.Created)
                .ToListAsync(cancellationToken);

            foreach (var s in shipments)
                s.StartClothingPreparation(_currentUser.UserId);

            await _context.SaveChangesAsync(cancellationToken);
            return shipments.Count;
        }
    }
}
