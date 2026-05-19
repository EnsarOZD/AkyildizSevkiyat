using Akyildiz.Sevkiyat.Application.External.YurtiKargo;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IYurtiKargoClient
    {
        Task<YkCreateShipmentResult> CreateShipmentAsync(YkCreateShipmentRequest request, CancellationToken ct = default);
        Task<bool> CancelShipmentAsync(string cargoKey, CancellationToken ct = default);
        Task<YkShipmentStatus?> QueryShipmentAsync(string cargoKey, CancellationToken ct = default);
    }
}
