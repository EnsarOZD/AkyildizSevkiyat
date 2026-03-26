namespace Akyildiz.Sevkiyat.Application.Settings
{
    public sealed class ShipmentSettings
    {
        /// <summary>
        /// true ise araç atama (AssignedToVehicle) geçişinde irsaliye numarası zorunludur.
        /// Netsis entegrasyonu aktif olduğunda true yapılması önerilir.
        /// </summary>
        public bool RequireIrsaliyeNoOnDispatch { get; init; } = false;
    }
}
