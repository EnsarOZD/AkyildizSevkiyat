namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum LocationType
    {
        Rack        = 0, // Raflı alan
        FloorStack  = 1, // Zemin istifleme
        Receiving   = 2, // Kabul / giriş alanı
        Shipping    = 3, // Sevkiyat / çıkış alanı
        Quarantine  = 4, // Karantina / hasarlı
        Staging     = 5, // Hazırlık / geçiş alanı
        PickingFace = 6, // Toplama gözü — alan bazlı sabit göz
        Returns     = 7, // İade deposu
    }
}
