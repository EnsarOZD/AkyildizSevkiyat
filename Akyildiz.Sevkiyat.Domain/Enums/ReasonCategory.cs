namespace Akyildiz.Sevkiyat.Domain.Enums
{
    /// <summary>
    /// Yönetilebilir sebep listelerinin kategorisi.
    /// Yeni bir akış için sebep tanımı gerektiğinde buraya yeni değer eklenir.
    /// </summary>
    public enum ReasonCategory
    {
        /// <summary>Sevkiyat hazırlık (picking) fark sebepleri — DifferenceReasonInput.</summary>
        PickingDifference = 0,

        /// <summary>Mal kabul / satınalma red sebepleri.</summary>
        GoodsReceiptReject = 1,
    }
}
