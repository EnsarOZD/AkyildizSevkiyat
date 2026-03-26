using Akyildiz.Sevkiyat.Application.Warehouse.Services;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Warehouse.Queries.GetPreDispatchSummary
{
    public record GetPreDispatchSummaryQuery(int ZonePreparationId) : IRequest<PreDispatchSummaryDto>;

    public class GetPreDispatchSummaryQueryHandler : IRequestHandler<GetPreDispatchSummaryQuery, PreDispatchSummaryDto>
    {
        private readonly PreDispatchGuard _preDispatchGuard;

        public GetPreDispatchSummaryQueryHandler(PreDispatchGuard preDispatchGuard)
        {
            _preDispatchGuard = preDispatchGuard;
        }

        public Task<PreDispatchSummaryDto> Handle(GetPreDispatchSummaryQuery request, CancellationToken cancellationToken)
        {
            return _preDispatchGuard.GetSummaryAsync(request.ZonePreparationId, cancellationToken);
        }
    }
}
