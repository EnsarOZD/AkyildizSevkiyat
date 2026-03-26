namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ReconciliationCheckType
    {
        /// <summary>ShipmentLine.OrderedQty ≠ IssOrderLine.OrderedQty</summary>
        IssQtyMismatch = 1,

        /// <summary>Teslim edilen sevkiyatta DeliveredQty = 0 olan satır var</summary>
        PickingIncomplete = 2,

        /// <summary>Teslim edildi ancak Netsis'e aktarılmadı (24h grace)</summary>
        NetsisTransferMissing = 3,

        /// <summary>Netsis'e aktarıldı ancak IrsaliyeNo 24 saat içinde gelmedi</summary>
        IrsaliyeMissing = 4,

        /// <summary>IssOrder aktarıldı ama satıra karşılık gelen aktif ShipmentLine yok</summary>
        IssCoverageGap = 5,
    }
}
