using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Tablo boşsa eski koda gömülü sebep listelerini başlangıç verisi olarak ekler.
    /// Idempotent: kayıt varsa hiçbir şey yapmaz (yöneticinin yaptığı düzenlemeleri ezmez).
    /// </summary>
    public static class DefinedReasonSeeder
    {
        public static async Task SeedAsync(IApplicationDbContext context)
        {
            if (await context.DefinedReasons.AnyAsync())
                return;

            var picking = new[] { "Stokta yok", "Kısmi stok", "Koli tamamlaması" };
            var reject = new[] { "Hasarlı", "Eksik / Kırık", "Yanlış Ürün", "Kalite Sorunu" };

            var seed = new List<DefinedReason>();
            for (int i = 0; i < picking.Length; i++)
                seed.Add(new DefinedReason { Category = ReasonCategory.PickingDifference, Label = picking[i], SortOrder = i, IsActive = true });
            for (int i = 0; i < reject.Length; i++)
                seed.Add(new DefinedReason { Category = ReasonCategory.GoodsReceiptReject, Label = reject[i], SortOrder = i, IsActive = true });

            context.DefinedReasons.AddRange(seed);
            await context.SaveChangesAsync(default);
        }
    }
}
