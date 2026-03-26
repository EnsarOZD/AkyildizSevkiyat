using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment
{
    // Şimdilik sadece IssOrderId ile çalışalım
    // Şimdilik sadece IssOrderId ile çalışalım
    public record CreateShipmentCommand : IRequest<int>, IRequireRoles
    {
        public int IssOrderId { get; init; }

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }
}
