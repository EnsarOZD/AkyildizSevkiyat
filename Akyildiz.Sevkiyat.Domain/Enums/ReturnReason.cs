namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ReturnReason
    {
        CustomerRejected = 0,   // Müşteri reddetti
        Damaged          = 1,   // Hasarlı
        ExcessLoading    = 2,   // Fazla yükleme
        WrongItem        = 3,   // Yanlış ürün
        ProjectNotFound  = 4,   // Proje bulunamadı / kapalıydı
        Other            = 99
    }
}
