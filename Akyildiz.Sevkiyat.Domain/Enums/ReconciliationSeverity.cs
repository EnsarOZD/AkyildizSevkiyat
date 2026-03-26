namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum ReconciliationSeverity
    {
        /// <summary>Dikkat gerektiriyor ama sistemi bloke etmiyor</summary>
        Warning = 1,

        /// <summary>Kritik tutarsızlık — müdahale gerekiyor</summary>
        Error = 2,
    }
}
