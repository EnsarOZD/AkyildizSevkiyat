using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ClothingPicking
{
    /// <summary>
    /// Kapama: BoxCount (zorunlu) + PackageType + opsiyonel not; ClosedBy*/ClosedAt set;
    /// Picking → ReadyForDispatch (mevcut geçiş). Sevkiyatın TÜM açık ContainerAssignment'ları
    /// otomatik release edilir. Kapamacı = herhangi personel (görev tipi, rol değil — K6).
    /// </summary>
    public record CompleteClosingCommand(int ShipmentId, int BoxCount, PackageType PackageType, string? Note)
        : IRequest<Unit>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager", "Accounting", "Warehouse" };
    }

    public class CompleteClosingCommandValidator : AbstractValidator<CompleteClosingCommand>
    {
        public CompleteClosingCommandValidator()
        {
            RuleFor(x => x.BoxCount).GreaterThan(0).WithMessage("Koli sayısı (BoxCount) 0'dan büyük olmalıdır.");
        }
    }

    public class CompleteClosingCommandHandler : IRequestHandler<CompleteClosingCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        public CompleteClosingCommandHandler(IApplicationDbContext c, ICurrentUserService u) { _context = c; _currentUser = u; }

        public async Task<Unit> Handle(CompleteClosingCommand request, CancellationToken ct)
        {
            var s = await _context.Shipments
                .Include(x => x.Lines)
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.Id == request.ShipmentId, ct)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (s.OperationType != OperationType.Clothing)
                throw new DomainException("Bu işlem yalnızca kıyafet sevkiyatları içindir.");
            if (s.Status != ShipmentStatus.Picking)
                throw new DomainException("Kapama yalnızca 'Hazırlanıyor' durumundaki sevkiyatlar için yapılır.");
            // Mod'dan bağımsız tek koşul: toplama bitmiş olmalı (Cart'a özgü koşul ARANMAZ).
            if (s.PickingCompletedAt == null)
                throw new DomainException("Kapama öncesi 'Toplama bitti' tamamlanmalıdır.");

            s.SetClosingInfo(_currentUser.FullName ?? _currentUser.Email ?? "Bilinmiyor",
                request.BoxCount, request.PackageType, request.Note);

            // Tüm açık arabaları otomatik boşalt
            var openContainers = await _context.ContainerAssignments
                .Where(a => a.ShipmentId == s.Id && a.ReleasedAt == null)
                .ToListAsync(ct);
            foreach (var a in openContainers)
            {
                a.ReleasedAt = DateTime.UtcNow;
                a.ReleaseReason = "Kapama tamamlandı";
            }

            // Mevcut geçiş — yeni durum yok.
            s.ChangeStatus(ShipmentStatus.ReadyForDispatch, _currentUser.UserId, "Kapama tamamlandı");

            // Eksik satırlardan ShortageRecord üret (Pending)
            _context.ShortageRecords.AddRange(ShortageRecordFactory.CreateForShipment(s, _currentUser.UserId));

            await _context.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
