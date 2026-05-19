namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ContainerType
    {
        Pallet = 0, // Palet — büyük hacimli raf konumu
        Case   = 1, // Koli — orta boy
        Box    = 2, // Kutu — toplama gözü (modül ortasındaki küçük pozisyonlar)
    }
}
