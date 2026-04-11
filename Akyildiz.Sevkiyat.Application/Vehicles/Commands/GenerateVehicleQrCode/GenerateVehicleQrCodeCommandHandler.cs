using MediatR;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;

namespace Akyildiz.Sevkiyat.Application.Vehicles.Commands.GenerateVehicleQrCode
{
    public class GenerateVehicleQrCodeCommandHandler
        : IRequestHandler<GenerateVehicleQrCodeCommand, GenerateVehicleQrCodeResult>
    {
        private readonly IApplicationDbContext _context;

        public GenerateVehicleQrCodeCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GenerateVehicleQrCodeResult> Handle(
            GenerateVehicleQrCodeCommand command,
            CancellationToken cancellationToken)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == command.VehicleId, cancellationToken)
                ?? throw new NotFoundException("Araç", command.VehicleId);

            vehicle.GenerateQrCode();
            await _context.SaveChangesAsync(cancellationToken);

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(vehicle.QrCode!, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(10);
            var qrImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";

            return new GenerateVehicleQrCodeResult(vehicle.QrCode!, qrImageBase64);
        }
    }
}
