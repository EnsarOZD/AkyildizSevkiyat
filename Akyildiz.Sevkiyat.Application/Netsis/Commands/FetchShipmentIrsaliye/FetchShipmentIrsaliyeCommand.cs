using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.External.Netsis.Dtos;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.FetchShipmentIrsaliye
{
    /// <summary>
    /// Belirtilen sevkiyat için Netsis'ten irsaliye numarasını çeker.
    /// Önkoşul: NetsisTransferredAt dolu olmalı.
    /// </summary>
    public record FetchShipmentIrsaliyeCommand(int ShipmentId) : IRequest<FetchShipmentIrsaliyeResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Dispatcher" };
    }

    public record FetchShipmentIrsaliyeResult(string? IrsaliyeNo, string Message);

    public class FetchShipmentIrsaliyeCommandHandler
        : IRequestHandler<FetchShipmentIrsaliyeCommand, FetchShipmentIrsaliyeResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly INetsisClient _netsisClient;

        public FetchShipmentIrsaliyeCommandHandler(IApplicationDbContext context, INetsisClient netsisClient)
        {
            _context = context;
            _netsisClient = netsisClient;
        }

        public async Task<FetchShipmentIrsaliyeResult> Handle(
            FetchShipmentIrsaliyeCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Project)
                .Include(s => s.IssOrder)
                .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
                ?? throw new NotFoundException("Shipment", request.ShipmentId);

            if (!shipment.NetsisTransferredAt.HasValue)
                throw new DomainException(
                    "İrsaliye çekimi için önce sevkiyatın Netsis'e aktarılmış olması gerekir.");

            var netsisOrderNo = shipment.IssOrder?.NetsisOrderNumber;
            if (string.IsNullOrWhiteSpace(netsisOrderNo))
                throw new DomainException("Sevkiyata ait Netsis sipariş numarası bulunamadı.");

            var cariKod = shipment.Project.NetsisCariKodu;
            if (string.IsNullOrWhiteSpace(cariKod))
                throw new DomainException(
                    $"Proje '{shipment.Project.Name}' için Netsis Cari Kodu tanımlanmamış.");

            var irsaliyeler = await _netsisClient.GetIrsaliyelerAsync(
                new NetsisIrsaliyeQuery { SiparisNo = netsisOrderNo, CariKod = cariKod },
                cancellationToken);

            if (irsaliyeler == null || !irsaliyeler.Any())
                return new FetchShipmentIrsaliyeResult(
                    null,
                    $"Netsis'te henüz irsaliye kesilmemiş. (Sipariş No: {netsisOrderNo})");

            var ilk = irsaliyeler.First();
            shipment.SetIrsaliyeInfo(ilk.IrsaliyeNo, ilk.IrsaliyeTarihi);

            // CLOTHING WORKAROUND: Kıyafet operasyonlarında irsaliye çekildiği an teslim edilmiş sayılır
            if (shipment.OperationType == Akyildiz.Sevkiyat.Domain.Enums.OperationType.Clothing && shipment.Status == Akyildiz.Sevkiyat.Domain.Enums.ShipmentStatus.Created)
            {
                shipment.SkipToDelivered();
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new FetchShipmentIrsaliyeResult(
                ilk.IrsaliyeNo,
                $"İrsaliye numarası güncellendi: {ilk.IrsaliyeNo}");
        }
    }
}
