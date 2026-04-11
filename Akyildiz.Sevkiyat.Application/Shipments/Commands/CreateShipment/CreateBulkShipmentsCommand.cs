using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CreateShipment
{
    public record BulkShipmentFailure(int IssOrderId, string Reason);

    public record CreateBulkShipmentsResult(
        int SuccessCount,
        int FailureCount,
        IReadOnlyList<BulkShipmentFailure> Failures);

    public class CreateBulkShipmentsCommand : IRequest<CreateBulkShipmentsResult>, IRequireRoles
    {
        public List<int> IssOrderIds { get; set; } = new();

        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class CreateBulkShipmentsCommandValidator : AbstractValidator<CreateBulkShipmentsCommand>
    {
        public CreateBulkShipmentsCommandValidator()
        {
            RuleFor(x => x.IssOrderIds)
                .NotNull().WithMessage("Sipariş listesi boş olamaz.")
                .Must(ids => ids.Count > 0).WithMessage("En az bir sipariş ID'si girilmelidir.")
                .Must(ids => ids.Count <= 200).WithMessage("Tek seferde en fazla 200 sipariş işlenebilir.");

            RuleForEach(x => x.IssOrderIds)
                .GreaterThan(0).WithMessage("Sipariş ID'si 0'dan büyük olmalıdır.");
        }
    }

    public class CreateBulkShipmentsCommandHandler : IRequestHandler<CreateBulkShipmentsCommand, CreateBulkShipmentsResult>
    {
        private readonly ISender _mediator;
        private readonly ILogger<CreateBulkShipmentsCommandHandler> _logger;

        public CreateBulkShipmentsCommandHandler(ISender mediator, ILogger<CreateBulkShipmentsCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<CreateBulkShipmentsResult> Handle(CreateBulkShipmentsCommand request, CancellationToken cancellationToken)
        {
            var failures = new List<BulkShipmentFailure>();
            int successCount = 0;

            foreach (var orderId in request.IssOrderIds)
            {
                try
                {
                    await _mediator.Send(new CreateShipmentCommand { IssOrderId = orderId }, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    var reason = ex.Message;
                    failures.Add(new BulkShipmentFailure(orderId, reason));
                    _logger.LogWarning(
                        "Toplu sevkiyat oluşturma: Sipariş #{OrderId} başarısız. Hata: {Reason}",
                        orderId, reason);
                }
            }

            _logger.LogInformation(
                "Toplu sevkiyat oluşturma tamamlandı. Başarılı: {Success}, Başarısız: {Failure}",
                successCount, failures.Count);

            return new CreateBulkShipmentsResult(successCount, failures.Count, failures);
        }
    }
}
