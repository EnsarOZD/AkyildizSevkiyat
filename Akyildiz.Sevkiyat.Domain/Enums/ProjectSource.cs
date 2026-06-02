namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>
    /// Project'in kaynak tipi — ISS-IP'den otomatik içe aktarılan projeler ile
    /// sistem içinde elle oluşturulan müşterileri ayırır.
    /// </summary>
    public enum ProjectSource
    {
        /// <summary>
        /// ISS-IP'den içe aktarılan proje. IssOrder akışı ve ISS karşılaştırma maili gibi
        /// ISS-spesifik özellikler bu projeler için aktiftir. NetsisTeslimCariKodu zorunludur.
        /// </summary>
        Iss = 0,

        /// <summary>
        /// Sistem içinden manuel oluşturulan müşteri. IssOrder bağı olmadan doğrudan
        /// sevkiyat oluşturulabilir. NetsisTeslimCariKodu opsiyoneldir.
        /// </summary>
        Manual = 1,
    }
}
