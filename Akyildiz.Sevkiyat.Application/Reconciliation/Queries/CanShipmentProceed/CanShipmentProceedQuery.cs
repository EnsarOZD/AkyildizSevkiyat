using Akyildiz.Sevkiyat.Application.Reconciliation.Services;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Reconciliation.Queries.CanShipmentProceed
{
    /// <summary>
    /// Sevkiyat için hangi operasyonların aktif engel taşıdığını döner.
    /// UI bunu kullanarak "Teslim Et", "Hazır", "Netsis'e Aktar" butonlarını devre dışı bırakabilir.
    /// </summary>
    public record CanShipmentProceedQuery(int ShipmentId) : IRequest<CanShipmentProceedResult>;

    public class CanShipmentProceedQueryHandler
        : IRequestHandler<CanShipmentProceedQuery, CanShipmentProceedResult>
    {
        private readonly ReconciliationGuard _guard;

        public CanShipmentProceedQueryHandler(ReconciliationGuard guard)
        {
            _guard = guard;
        }

        public Task<CanShipmentProceedResult> Handle(
            CanShipmentProceedQuery request, CancellationToken ct)
            => _guard.GetBlockingIssuesAsync(request.ShipmentId, ct);
    }
}
