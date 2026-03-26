namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ZonePreparationStatus
    {
        Draft = 0,
        MicroPicking = 1,
        MicroReady = 2,
        MacroPicking = 3,
        ReadyForDriverInfo = 4,
        ReadyForTransfer = 5,
        Dispatched = 6        // Yükleme onaylandı, araç yola çıktı
    }
}
