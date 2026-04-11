using MediatR;

namespace Akyildiz.Sevkiyat.Application.Vehicles.Commands.GenerateVehicleQrCode
{
    public record GenerateVehicleQrCodeCommand(int VehicleId) : IRequest<GenerateVehicleQrCodeResult>;

    public record GenerateVehicleQrCodeResult(string QrCode, string QrImageBase64);
}
