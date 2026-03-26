namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.Netsis
{
    /// <summary>
    /// Singleton — Netsis token'ını in-memory cache'de tutar.
    /// Token süresi dolmadan önce yenileme yapılır (bkz. NetsisOptions.TokenExpiryMinutes).
    /// </summary>
    public sealed class NetsisTokenCache
    {
        private string? _token;
        private DateTime _expiresAt = DateTime.MinValue;

        public SemaphoreSlim Lock { get; } = new(1, 1);

        public string? Token => IsValid ? _token : null;

        private bool IsValid => _token != null && DateTime.UtcNow < _expiresAt;

        public void SetToken(string token, int expiryMinutes)
        {
            _token     = token;
            _expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        }

        public void Invalidate() => _token = null;
    }
}
