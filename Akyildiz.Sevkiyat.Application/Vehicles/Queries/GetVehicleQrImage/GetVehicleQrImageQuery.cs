using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Akyildiz.Sevkiyat.Application.Vehicles.Queries.GetVehicleQrImage
{
    public record GetVehicleQrImageQuery(int VehicleId) : IRequest<GetVehicleQrImageResult>;

    public record GetVehicleQrImageResult(string QrCode, string QrImageBase64);

    public class GetVehicleQrImageQueryHandler : IRequestHandler<GetVehicleQrImageQuery, GetVehicleQrImageResult>
    {
        private readonly IApplicationDbContext _context;

        public GetVehicleQrImageQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetVehicleQrImageResult> Handle(GetVehicleQrImageQuery request, CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
                ?? throw new NotFoundException("Araç", request.VehicleId);

            if (string.IsNullOrEmpty(vehicle.QrCode))
                throw new DomainException("Bu araç için henüz QR kod oluşturulmamış.");

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(vehicle.QrCode, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(10);
            var qrImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";

            return new GetVehicleQrImageResult(vehicle.QrCode, qrImageBase64);
        }
    }
}
