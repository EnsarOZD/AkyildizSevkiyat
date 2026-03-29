using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Services
{
    /// <summary>
    /// Rota sıralama servisi: Boğaz geçişine göre Avrupa/Anadolu ayırımı,
    /// nearest-neighbor sıralama, pencere ihlali düzeltmesi (Haversine-tabanlı
    /// ön hesaplama), köprü waypoint enjeksiyonu.
    /// </summary>
    public class RouteOrderingService
    {
        private const double BosphorusLongitude = 29.05;
        private const double AvgSpeedKmh        = 50.0;   // şehir içi ortalama
        private const double DwellMinutes        = 15.0;   // durak bekleme süresi
        private const double BridgeCrossingMin   = 10.0;   // köprü geçiş tahmini

        // Anatolian district keywords for address-based fallback
        private static readonly HashSet<string> AnatolianKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "KADIKÖY", "KADIKOY", "ÜSKÜDAR", "USKUDAR", "KARTAL", "MALTEPE",
            "PENDİK", "PENDIK", "TUZLA", "GEBZE", "KOCAELİ", "KOCAELI",
            "İZMİT", "IZMIT", "ANADOLU", "ATAŞEHIR", "ATASEHIR", "ÜMRANIYE",
            "UMRANIYE", "ÇEKMEKÖY", "CEKMEKOY", "BEYKOZ", "ŞİLE", "SILE",
            "SULTANBEYLI", "SANCAKTEPE"
        };

        private static readonly Dictionary<string, (string Code, string Name, string Address)> BridgeWaypoints =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Kamyon"]   = ("__BRIDGE__", "Yavuz Sultan Selim Köprüsü", "Yavuz Sultan Selim Köprüsü, İstanbul"),
                ["Kamyonet"] = ("__BRIDGE__", "Fatih Sultan Mehmet Köprüsü", "Fatih Sultan Mehmet Köprüsü, İstanbul"),
                ["Minibüs"]  = ("__BRIDGE__", "Fatih Sultan Mehmet Köprüsü", "Fatih Sultan Mehmet Köprüsü, İstanbul"),
            };

        public record StopInfo(
            string Code,
            string Name,
            string Address,
            double? Latitude,
            double? Longitude,
            TimeOnly? DeliveryWindowStart,
            TimeOnly? DeliveryWindowEnd
        );

        /// <summary>Ön hesaplama uyarısı — Google API öncesi Haversine tabanlı.</summary>
        public record PreliminaryWarning(
            string ProjectCode,
            string ProjectName,
            TimeOnly WindowStart,
            TimeOnly WindowEnd,
            TimeOnly EstimatedArrival,
            bool IsLate,
            string WarningType   // "EarlyArrival" | "LateArrival"
        );

        public record OrderedRoute(
            List<StopInfo> Stops,              // bridge marker'ları dahil sıralı duraklar
            bool HasBridgeCrossing,
            string? BridgeNotice,
            List<PreliminaryWarning> PreliminaryWarnings
        );

        /// <summary>
        /// Durakları sıralar:
        /// 1. Her taraf için nearest-neighbor (farthest-first)
        /// 2. Her taraf için Haversine tabanlı pencere ihlali düzeltmesi
        /// 3. Köprü waypoint enjeksiyonu
        /// </summary>
        public OrderedRoute OrderStops(
            List<StopInfo> stops,
            double? startLatitude,
            double? startLongitude,
            string? vehicleType,
            bool forceBridgeCrossing,   // API uyumluluğu için tutuldu, kullanılmıyor
            bool returnToStart,
            TimeOnly? departureTime = null)
        {
            if (stops.Count == 0)
                return new OrderedRoute(stops, false, null, []);

            // 1. Boğaz'a göre ayır
            var europeStops   = stops.Where(s =>  IsEurope(s)).ToList();
            var anatoliaStops = stops.Where(s => !IsEurope(s)).ToList();

            // 2. Başlangıç tarafını belirle (koordinat yoksa Avrupa varsayılır)
            bool hasStartCoords = startLongitude.HasValue;
            bool startIsEurope  = !hasStartCoords || startLongitude!.Value < BosphorusLongitude;

            // 3. Köprü gereksinimi
            bool needsBridge = hasStartCoords
                ? (startIsEurope ? anatoliaStops.Count > 0 : europeStops.Count > 0)
                : (europeStops.Count > 0 && anatoliaStops.Count > 0);

            // 4. Köprü durağı
            StopInfo? bridgeStop = null;
            if (needsBridge && !string.IsNullOrWhiteSpace(vehicleType) &&
                BridgeWaypoints.TryGetValue(vehicleType, out var bridge))
                bridgeStop = new StopInfo(bridge.Code, bridge.Name, bridge.Address, null, null, null, null);

            var firstSideStops  = startIsEurope ? europeStops   : anatoliaStops;
            var secondSideStops = startIsEurope ? anatoliaStops : europeStops;

            double startLat = startLatitude  ?? 41.0;
            double startLon = startLongitude ?? 28.9;
            var effectiveDeparture = departureTime ?? new TimeOnly(8, 0);

            // 5. İlk taraf: sırala + pencere düzelt
            var sortedFirst = SortSide(firstSideStops, startLatitude, startLongitude, null);
            var (fixedFirst, warningsFirst) = ApplyWindowFixes(
                sortedFirst, startLat, startLon, effectiveDeparture);

            // 6. İkinci taraf için başlangıç zamanını tahmin et
            double firstEndMin = ComputeCumulativeMinutes(
                fixedFirst, startLat, startLon,
                effectiveDeparture.Hour * 60.0 + effectiveDeparture.Minute);
            double secondStartMin = firstEndMin + (bridgeStop != null ? BridgeCrossingMin : 0.0);
            var secondDeparture = TimeOnly.FromTimeSpan(
                TimeSpan.FromMinutes(secondStartMin % (24 * 60)));

            // 7. İkinci taraf referans noktası (köprü çıkış koordinatı)
            double? secondRefLat = null, secondRefLon = null;
            if (bridgeStop != null)
            {
                if (bridgeStop.Name.Contains("Yavuz")) { secondRefLat = 41.2027; secondRefLon = 29.0656; }
                else                                    { secondRefLat = 41.0882; secondRefLon = 29.0594; }
            }
            var sortedSecond = SortSide(secondSideStops, secondRefLat, secondRefLon,
                bridgeStop == null ? GetSideCenter(firstSideStops) : null);

            double secStartLat = secondRefLat ?? GetSideCenter(firstSideStops)?.Lat ?? startLat;
            double secStartLon = secondRefLon ?? GetSideCenter(firstSideStops)?.Lon ?? startLon;
            var (fixedSecond, warningsSecond) = ApplyWindowFixes(
                sortedSecond, secStartLat, secStartLon, secondDeparture);

            // 8. Sonuç: birinci taraf → [köprü] → ikinci taraf → [geri dönüş köprüsü]
            var result = new List<StopInfo>(fixedFirst.Count + fixedSecond.Count + 2);
            result.AddRange(fixedFirst);
            if (bridgeStop != null) result.Add(bridgeStop);
            result.AddRange(fixedSecond);
            if (returnToStart && bridgeStop != null) result.Add(bridgeStop);

            var allWarnings = new List<PreliminaryWarning>(warningsFirst.Count + warningsSecond.Count);
            allWarnings.AddRange(warningsFirst);
            allWarnings.AddRange(warningsSecond);

            return new OrderedRoute(result, bridgeStop != null, bridgeStop?.Name, allWarnings);
        }

        // ── Pencere ihlali düzeltmesi ─────────────────────────────────────────

        /// <summary>
        /// Haversine tahminleriyle pencere ihlallerini tespit eder ve durakları taşır:
        /// - Erken varış → sona taşı (window açılana kadar diğerleri yapılır)
        /// - Geç varış   → öne taşı (bir an önce ulaşılmaya çalışılır)
        /// Taşıma sonrası yeni varışları hesaplar ve ön uyarıları üretir.
        /// </summary>
        private static (List<StopInfo> Fixed, List<PreliminaryWarning> Warnings) ApplyWindowFixes(
            List<StopInfo> stops,
            double startLat, double startLon,
            TimeOnly departureTime)
        {
            if (stops.Count == 0)
                return (stops, []);

            // Adım 1: İlk sıralamaya göre tahmini varışları hesapla
            var arrivals = EstimateArrivals(stops, startLat, startLon, departureTime);

            // Adım 2: İhlalleri sınıflandır
            var earlyStops  = new List<StopInfo>();   // erken → sona
            var lateStops   = new List<StopInfo>();   // geç   → öne
            var normalStops = new List<StopInfo>();

            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                if (!stop.DeliveryWindowStart.HasValue || !stop.DeliveryWindowEnd.HasValue)
                {
                    normalStops.Add(stop);
                    continue;
                }
                if (arrivals[i] < stop.DeliveryWindowStart.Value)
                    earlyStops.Add(stop);
                else if (arrivals[i] > stop.DeliveryWindowEnd.Value)
                    lateStops.Add(stop);
                else
                    normalStops.Add(stop);
            }

            // Adım 3: Yeni sıra: geç öne + normal + erken sona
            var reordered = new List<StopInfo>(stops.Count);
            reordered.AddRange(lateStops);
            reordered.AddRange(normalStops);
            reordered.AddRange(earlyStops);

            // Adım 4: Yeni sıralamaya göre varışları yeniden hesapla
            var finalArrivals = EstimateArrivals(reordered, startLat, startLon, departureTime);

            // Adım 5: Ön uyarıları üret
            var warnings = new List<PreliminaryWarning>();
            for (int i = 0; i < reordered.Count; i++)
            {
                var stop = reordered[i];
                if (!stop.DeliveryWindowStart.HasValue || !stop.DeliveryWindowEnd.HasValue)
                    continue;

                var arrival = finalArrivals[i];
                if (arrival < stop.DeliveryWindowStart.Value)
                    warnings.Add(new PreliminaryWarning(
                        stop.Code, stop.Name,
                        stop.DeliveryWindowStart.Value, stop.DeliveryWindowEnd.Value,
                        arrival, false, "EarlyArrival"));
                else if (arrival > stop.DeliveryWindowEnd.Value)
                    warnings.Add(new PreliminaryWarning(
                        stop.Code, stop.Name,
                        stop.DeliveryWindowStart.Value, stop.DeliveryWindowEnd.Value,
                        arrival, true, "LateArrival"));
            }

            return (reordered, warnings);
        }

        /// <summary>Haversine + 50 km/h + 15 dk bekleme ile tahmini varış listesi.</summary>
        private static List<TimeOnly> EstimateArrivals(
            List<StopInfo> stops, double startLat, double startLon, TimeOnly departure)
        {
            var arrivals = new List<TimeOnly>(stops.Count);
            double curLat = startLat, curLon = startLon;
            double minutes = departure.Hour * 60.0 + departure.Minute;

            foreach (var stop in stops)
            {
                double lat = stop.Latitude  ?? curLat;
                double lon = stop.Longitude ?? curLon;
                minutes += (Haversine(curLat, curLon, lat, lon) / AvgSpeedKmh) * 60.0;
                arrivals.Add(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes % (24 * 60))));
                minutes += DwellMinutes;
                curLat = lat; curLon = lon;
            }

            return arrivals;
        }

        /// <summary>Durak listesi bitimindeki kümülatif dakikayı döner.</summary>
        private static double ComputeCumulativeMinutes(
            List<StopInfo> stops, double startLat, double startLon, double startMinutes)
        {
            double curLat = startLat, curLon = startLon;
            double minutes = startMinutes;
            foreach (var stop in stops)
            {
                double lat = stop.Latitude  ?? curLat;
                double lon = stop.Longitude ?? curLon;
                minutes += (Haversine(curLat, curLon, lat, lon) / AvgSpeedKmh) * 60.0
                           + DwellMinutes;
                curLat = lat; curLon = lon;
            }
            return minutes;
        }

        // ── Nearest-neighbor sıralama ─────────────────────────────────────────

        /// <summary>
        /// Bir tarafın duraklarını referans noktasından başlayarak sıralar.
        /// Pencere gruplamadan arındırılmış; pencere düzeltmesi ApplyWindowFixes'ta yapılır.
        /// </summary>
        private static List<StopInfo> SortSide(
            List<StopInfo> stops,
            double? refLat,
            double? refLon,
            (double Lat, double Lon)? altRef)
        {
            if (stops.Count == 0) return stops;

            double startLat = refLat ?? altRef?.Lat ?? stops[0].Latitude ?? 41.0;
            double startLon = refLon ?? altRef?.Lon ?? stops[0].Longitude ?? 28.9;

            (var ordered, _, _) = NearestNeighborChain(stops, startLat, startLon);
            return ordered;
        }

        /// <summary>
        /// Nearest-neighbor zinciri: ilk durak en uzaktan başlar,
        /// sonraki duraklar en yakın olanı seçer.
        /// </summary>
        private static (List<StopInfo> Ordered, double ExitLat, double ExitLon) NearestNeighborChain(
            List<StopInfo> stops, double curLat, double curLon)
        {
            var remaining = new List<StopInfo>(stops);
            var ordered   = new List<StopInfo>(stops.Count);

            // En uzak durarakla başla
            var first = remaining
                .OrderByDescending(s => Haversine(curLat, curLon,
                    s.Latitude  ?? curLat,
                    s.Longitude ?? curLon))
                .First();

            ordered.Add(first);
            remaining.Remove(first);
            curLat = first.Latitude  ?? curLat;
            curLon = first.Longitude ?? curLon;

            while (remaining.Count > 0)
            {
                var nearest = remaining
                    .OrderBy(s => Haversine(curLat, curLon,
                        s.Latitude  ?? curLat,
                        s.Longitude ?? curLon))
                    .First();

                ordered.Add(nearest);
                remaining.Remove(nearest);
                curLat = nearest.Latitude  ?? curLat;
                curLon = nearest.Longitude ?? curLon;
            }

            return (ordered, curLat, curLon);
        }

        // ── Yardımcı metotlar ─────────────────────────────────────────────────

        private static bool IsEurope(StopInfo stop)
        {
            if (stop.Longitude.HasValue)
                return stop.Longitude.Value < BosphorusLongitude;

            if (!string.IsNullOrWhiteSpace(stop.Address))
            {
                var upper = stop.Address.ToUpperInvariant()
                    .Replace("İ", "I").Replace("Ş", "S").Replace("Ğ", "G")
                    .Replace("Ü", "U").Replace("Ö", "O").Replace("Ç", "C");
                foreach (var kw in AnatolianKeywords)
                {
                    var kwNorm = kw.ToUpperInvariant()
                        .Replace("İ", "I").Replace("Ş", "S").Replace("Ğ", "G")
                        .Replace("Ü", "U").Replace("Ö", "O").Replace("Ç", "C");
                    if (upper.Contains(kwNorm)) return false;
                }
            }

            return true;
        }

        private static (double Lat, double Lon)? GetSideCenter(List<StopInfo> stops)
        {
            var withCoords = stops.Where(s => s.Latitude.HasValue && s.Longitude.HasValue).ToList();
            if (withCoords.Count == 0) return null;
            return (withCoords.Average(s => s.Latitude!.Value),
                    withCoords.Average(s => s.Longitude!.Value));
        }

        public static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
