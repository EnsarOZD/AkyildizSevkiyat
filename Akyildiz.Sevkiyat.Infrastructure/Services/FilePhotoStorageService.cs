using System.Text.RegularExpressions;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Akyildiz.Sevkiyat.Infrastructure.Services
{
    public class FilePhotoStorageService : IPhotoStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<FilePhotoStorageService> _logger;

        public FilePhotoStorageService(IConfiguration configuration, ILogger<FilePhotoStorageService> logger)
        {
            _basePath = configuration["PhotoStorage:BasePath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "photos");
            _logger = logger;
        }

        public async Task<string> SaveAsync(string base64, string category, CancellationToken ct = default)
        {
            // Strip data URI prefix if present
            var idx = base64.IndexOf(',');
            if (idx >= 0) base64 = base64[(idx + 1)..];

            var monthDir = DateTime.UtcNow.ToString("yyyy-MM");
            var relativePath = Path.Combine(category, monthDir, $"{Guid.NewGuid():N}.jpg");
            var fullPath = Path.Combine(_basePath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            var bytes = Convert.FromBase64String(base64);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            return relativePath.Replace('\\', '/');
        }

        public async Task<string> SaveDeliveryPhotoAsync(string base64, int shipmentId, string? irsaliyeNo, int photoIndex, CancellationToken ct = default)
        {
            var idx = base64.IndexOf(',');
            if (idx >= 0) base64 = base64[(idx + 1)..];

            var now = DateTime.UtcNow;
            var sanitizedIrsaliye = string.IsNullOrWhiteSpace(irsaliyeNo)
                ? "noIRS"
                : Regex.Replace(irsaliyeNo, @"[^a-zA-Z0-9\-_]", "_");
            var shortGuid = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"{shipmentId}_{sanitizedIrsaliye}_{photoIndex}_{shortGuid}.jpg";
            var relativePath = $"delivery/{now:yyyy}/{now:MM}/{now:dd}/{fileName}";
            var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            var bytes = Convert.FromBase64String(base64);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            return relativePath;
        }

        public Task DeleteAsync(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fotoğraf silinirken hata (best-effort): {Path}", relativePath);
            }
            return Task.CompletedTask;
        }

        public string GetUrl(string relativePath) => $"/photos/{relativePath.Replace('\\', '/')}";
    }
}
