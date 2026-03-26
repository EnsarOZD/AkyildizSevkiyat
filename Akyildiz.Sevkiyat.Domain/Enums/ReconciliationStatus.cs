namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ReconciliationStatus
    {
        /// <summary>Tespit edildi, henüz incelenmedi</summary>
        Open = 0,

        /// <summary>Admin tarafından görüldü ve not eklendi</summary>
        Acknowledged = 1,

        /// <summary>Bir sonraki kontrol çalışmasında sorun kendiliğinden ortadan kalktı</summary>
        AutoResolved = 2,
    }
}
