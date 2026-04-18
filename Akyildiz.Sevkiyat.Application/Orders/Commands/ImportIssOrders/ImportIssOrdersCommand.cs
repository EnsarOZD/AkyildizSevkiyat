using Akyildiz.Sevkiyat.Application.Common;
using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using System.Diagnostics;

namespace Akyildiz.Sevkiyat.Application.Orders.Commands.ImportIssOrders
{
    public record ImportIssOrdersCommand(DateTime StartDate, DateTime EndDate) : IRequest<ImportIssOrdersResult>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting", "Driver" };
    }

    public class ImportIssOrdersResult
    {
        public int TotalFromIss { get; set; }
        public int NewCount { get; set; }
        public int SkippedCount { get; set; }      // Mevcut sipariş — atlandı
        public int NeedsMappingCount { get; set; } // Yeni ama eşleştirme bekliyor
        public int FailedCount { get; set; }        // Parse/kayıt hatası
        public int BatchId { get; set; }            // ImportBatch kaydı
        public List<string> Errors { get; set; } = new();
    }

}
