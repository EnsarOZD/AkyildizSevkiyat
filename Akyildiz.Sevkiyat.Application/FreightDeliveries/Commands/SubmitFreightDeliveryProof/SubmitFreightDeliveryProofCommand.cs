using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Shipments.Commands.MarkShipmentDelivered;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FreightDeliveries.Commands.SubmitFreightDeliveryProof
{
    /// <summary>
    /// Nakliyecinin public linkten teslim kanıtı yüklemesi: en fazla 5 foto + teslim alan adı.
    /// İlgili projedeki tüm sevkiyatları teslim edildi olarak işaretler (MarkShipmentDelivered yeniden kullanılır).
    /// </summary>
    public record SubmitFreightDeliveryProofCommand(
        string Token,
        string RecipientName,
        string? Note,
        List<string> PhotosBase64
    ) : IRequest<SubmitFreightDeliveryProofResult>;

    public record SubmitFreightDeliveryProofResult(int DeliveredCount);

    public class SubmitFreightDeliveryProofCommandValidator : AbstractValidator<SubmitFreightDeliveryProofCommand>
    {
        public SubmitFreightDeliveryProofCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.RecipientName).NotEmpty().WithMessage("Teslim alan bilgisi zorunludur.").MaximumLength(200);
            RuleFor(x => x.PhotosBase64)
                .NotEmpty().WithMessage("En az bir teslim fotoğrafı zorunludur.")
                .Must(p => p.Count <= 5).WithMessage("En fazla 5 fotoğraf yükleyebilirsiniz.");
        }
    }

    public class SubmitFreightDeliveryProofCommandHandler
        : IRequestHandler<SubmitFreightDeliveryProofCommand, SubmitFreightDeliveryProofResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;

        public SubmitFreightDeliveryProofCommandHandler(IApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<SubmitFreightDeliveryProofResult> Handle(
            SubmitFreightDeliveryProofCommand request,
            CancellationToken cancellationToken)
        {
            var delivery = await _context.FreightDeliveries
                .Include(d => d.Shipments)
                .FirstOrDefaultAsync(d => d.Token == request.Token, cancellationToken)
                ?? throw new NotFoundException("Teslim linki bulunamadı veya geçersiz.");

            if (delivery.IsCompleted)
                throw new ConflictException("Bu teslim zaten tamamlanmış.");

            if (delivery.IsExpired)
                throw new DomainException("Teslim linkinin süresi dolmuş. Lütfen yetkiliyle iletişime geçin.");

            var photos = request.PhotosBase64.Where(p => !string.IsNullOrWhiteSpace(p)).Take(5).ToList();
            if (photos.Count == 0)
                throw new DomainException("En az bir teslim fotoğrafı zorunludur.");

            int deliveredCount = 0;
            foreach (var link in delivery.Shipments)
            {
                // MarkShipmentDelivered tüm yan etkileri yönetir (stok çıkışı, foto, durum, idempotency).
                // Anonim bağlamda 'override' dalına düşer → OverrideNote zorunlu.
                await _mediator.Send(new MarkShipmentDeliveredCommand(
                    ShipmentId: link.ShipmentId,
                    DeliveryNote: request.Note,
                    DeliveryRecipient: request.RecipientName,
                    DeliveryPhotosBase64: photos,
                    OverrideNote: $"Nakliye teslimi — taşıyıcı: {delivery.CarrierName}"
                ), cancellationToken);
                deliveredCount++;
            }

            delivery.Complete(request.RecipientName, request.Note);
            await _context.SaveChangesAsync(cancellationToken);

            return new SubmitFreightDeliveryProofResult(deliveredCount);
        }
    }
}
