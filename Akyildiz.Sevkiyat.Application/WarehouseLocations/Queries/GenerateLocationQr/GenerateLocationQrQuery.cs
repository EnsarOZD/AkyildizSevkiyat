using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Queries.GenerateLocationQr
{
    public record GenerateLocationQrQuery(int LocationId) : IRequest<LocationQrResult>;

    public record LocationQrResult(string QrValue, string QrImageBase64);

    public class GenerateLocationQrQueryHandler
        : IRequestHandler<GenerateLocationQrQuery, LocationQrResult>
    {
        private readonly IApplicationDbContext _context;
        public GenerateLocationQrQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<LocationQrResult> Handle(
            GenerateLocationQrQuery request,
            CancellationToken cancellationToken)
        {
            var loc = await _context.WarehouseLocations
                .FirstOrDefaultAsync(l => l.Id == request.LocationId, cancellationToken)
                ?? throw new NotFoundException("Lokasyon bulunamadı.");

            // PickingFace uses Code directly; Rack/others use module-level QrCode (auto-set if missing)
            string qrValue;
            if (loc.LocationType == LocationType.PickingFace)
            {
                qrValue = loc.Code;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(loc.QrCode))
                {
                    loc.QrCode = $"{loc.KoridorNo}{loc.Taraf}-{loc.ModulNo:D3}";
                    await _context.SaveChangesAsync(cancellationToken);
                }
                qrValue = loc.QrCode;
            }

            var gen   = new QRCodeGenerator();
            var data  = gen.CreateQrCode(qrValue, QRCodeGenerator.ECCLevel.Q);
            var png   = new PngByteQRCode(data);
            var bytes = png.GetGraphic(10);

            return new LocationQrResult(qrValue, $"data:image/png;base64,{Convert.ToBase64String(bytes)}");
        }
    }
}
