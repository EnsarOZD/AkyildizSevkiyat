using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.RevertToDraft;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.SendComparisonEmail;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Application.Shipments.Commands.CancelShipment
{
    /// <summary>
    /// Sevkiyatı sebep girilerek iptal eder (pasife alır). Depo hazırlık aşamasındaki
    /// sevkiyatlarda rezervasyon mevcut "taslağa al" akışıyla serbest bırakılır.
    /// "Stokta yok" sebebinde projeye eksik/iptal bildirim e-postası gönderilir.
    /// </summary>
    public record CancelShipmentCommand(
        int ShipmentId,
        string Reason,
        bool NotifyOutOfStock = false,
        List<string>? ExtraCc = null)
        : IRequest<CancelShipmentResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public record CancelShipmentResult(bool EmailSent, string? EmailError);

    public class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
    {
        public CancelShipmentCommandValidator()
        {
            RuleFor(x => x.ShipmentId).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("İptal sebebi zorunludur.")
                .MaximumLength(500);
        }
    }

    public class CancelShipmentCommandHandler
        : IRequestHandler<CancelShipmentCommand, CancelShipmentResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ISender _mediator;
        private readonly ILogger<CancelShipmentCommandHandler> _logger;

        public CancelShipmentCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            ISender mediator,
            ILogger<CancelShipmentCommandHandler> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<CancelShipmentResult> Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (shipment.NetsisTransferredAt != null)
                throw new DomainException(
                    "Netsis'e aktarılmış sevkiyat bu ekrandan iptal edilemez. " +
                    "Önce Netsis irsaliyesini iptal edin.");

            var cancellableStatuses = new[]
            {
                ShipmentStatus.Created,
                ShipmentStatus.AssignedToWarehouse,
                ShipmentStatus.Picking,
                ShipmentStatus.ReadyForDispatch,
            };

            if (!cancellableStatuses.Contains(shipment.Status))
                throw new DomainException(
                    $"Bu sevkiyat iptal edilemez. Mevcut durum: {shipment.Status}. " +
                    "İptal yalnızca Taslak, Depoya Atandı, Hazırlanıyor veya Hazır durumlarında yapılabilir.");

            // Depo hazırlık aşamalarında rezervasyonu serbest bırakmak için mevcut, test edilmiş
            // "taslağa al" akışını yeniden kullan. Bu, sevkiyatı Created durumuna çeker, picking
            // verilerini ve bölge bağını temizler. Created ise (taslak) rezervasyon yoktur, atlanır.
            if (shipment.Status != ShipmentStatus.Created)
            {
                await _mediator.Send(
                    new RevertToDraftCommand(request.ShipmentId, $"İptal: {request.Reason}"),
                    cancellationToken);
            }

            // Created → pasife al + iptal sebebini sakla.
            shipment.SetPassive(_currentUser.UserId, request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            // "Stokta yok" sebebinde projeye bildirim gönder. Hata olursa iptal yine de başarılı sayılır.
            bool emailSent = false;
            string? emailError = null;
            if (request.NotifyOutOfStock)
            {
                try
                {
                    await _mediator.Send(
                        new SendComparisonEmailCommand(request.ShipmentId, request.ExtraCc, CancellationReason: request.Reason),
                        cancellationToken);
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    emailError = ex.Message;
                    _logger.LogWarning(ex,
                        "İptal bildirimi e-postası gönderilemedi. Sevkiyat #{ShipmentId}", request.ShipmentId);
                }
            }

            return new CancelShipmentResult(emailSent, emailError);
        }
    }
}
