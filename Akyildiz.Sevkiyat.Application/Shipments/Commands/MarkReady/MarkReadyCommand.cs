using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkReady
{
    public record MarkReadyCommand(int ShipmentId, string? Reason = null) : IRequest<MarkReadyCommandResponse>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Warehouse", "Dispatcher" };
    }

    public class MarkReadyCommandResponse
    {
        public List<string> Warnings { get; set; } = new();
    }
}
