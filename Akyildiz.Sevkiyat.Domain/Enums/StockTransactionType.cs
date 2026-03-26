namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum StockTransactionType
    {
        GoodsIn        = 0,  // Mal girişi (GoodsReceipt posted)
        ShipmentOut    = 1,  // Sevkiyat araç yüklemesi (AssignVehicle)
        ManualAdjust   = 2,  // Manuel düzeltme
        Reserve        = 3,  // Sevkiyat için rezervasyon (AssignToWarehouse)
        ReleaseReserve = 4,  // Rezervasyon iptali (RevertToDraft / Cancel)
        VehicleReturn  = 5,  // Araçtan geri dönen ürün
        GoodsInCorrection = 6  // Mal girişi düzeltmesi (negatif miktar, Posted GR iptali)
    }
}
