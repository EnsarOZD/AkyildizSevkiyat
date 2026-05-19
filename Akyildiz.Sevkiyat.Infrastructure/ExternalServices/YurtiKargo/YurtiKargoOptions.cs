namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.YurtiKargo
{
    public sealed class YurtiKargoOptions
    {
        public string BaseUrl { get; init; } = "https://testws.yurticikargo.com/KOPSWebServices/ShippingOrderDispatcherServices";

        public string WsUserName { get; init; } = string.Empty;

        public string WsPassword { get; init; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(WsUserName) && !WsUserName.StartsWith("SET_BY_ENV_") &&
            !string.IsNullOrWhiteSpace(WsPassword) && !WsPassword.StartsWith("SET_BY_ENV_");

        /// <summary>
        /// Ödeme tipi: 1=Gönderici ödemeli, 2=Alıcı ödemeli, 3=Karşı ödemeli
        /// </summary>
        public int PaymentType { get; init; } = 1;

        /// <summary>
        /// Kargo takip anahtarı prefix'i — {prefix}{shipmentId} formatında üretilir.
        /// </summary>
        public string CargoKeyPrefix { get; init; } = "AKY-";

        public int TimeoutSeconds { get; init; } = 30;

        /// <summary>
        /// Opsiyonel proxy URL — yerel geliştirmede whitelist IP sorunu için.
        /// Örnek: "socks5://127.0.0.1:8888" (SSH dynamic tunnel)
        /// Production'da boş bırakılır.
        /// </summary>
        public string? ProxyUrl { get; init; }
    }
}
