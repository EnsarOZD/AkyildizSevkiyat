using MediatR;

namespace Akyildiz.Sevkiyat.Application.Netsis.Commands.SyncIrsaliyeFromNetsis
{
    /// <summary>
    /// Delivered durumundaki, Netsis'e aktarılmış ama irsaliye bilgisi henüz alınmamış
    /// sevkiyatlar için Netsis'ten irsaliye numarası ve tarihini çeker.
    /// </summary>
    public record SyncIrsaliyeFromNetsisCommand : IRequest<SyncIrsaliyeFromNetsisResult>;

    public class SyncIrsaliyeFromNetsisResult
    {
        public int SyncedCount    { get; set; } // İrsaliye bilgisi güncellenen sevkiyat sayısı
        public int NotFoundCount  { get; set; } // Netsis'te irsaliye bulunamayan sevkiyat sayısı
        public int SkippedCount   { get; set; } // Aktarılmamış / NetsisOrderNumber boş olanlar
    }
}
