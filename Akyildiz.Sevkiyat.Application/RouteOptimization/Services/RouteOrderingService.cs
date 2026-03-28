using Akyildiz.Sevkiyat.Application.RouteOptimization.Dtos;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Application.RouteOptimization.Services
{
    /// <summary>
    /// Rota sıralama servisi: Boğaz geçişine göre Avrupa/Anadolu ayırımı,
    /// teslimat penceresi sıralaması, farthest-first, köprü waypoint enjeksiyonu.
    /// </summary>
    public class RouteOrderingService
    {
        private const double BosphorusLongitude = 29.05;

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

        public record OrderedRoute(
            List<StopInfo> Stops,        // ordered stops including bridge if needed
            bool HasBridgeCrossing,
            string? BridgeNotice
        );

        /// <summary>
        /// Orders stops using Europe/Anatolia split, delivery windows, and farthest-first.
        /// </summary>
        public OrderedRoute OrderStops(
            List<StopInfo> stops,
            double? startLatitude,
            double? startLongitude,
            string? vehicleType,
            bool forceBridgeCrossing,
            bool returnToStart)
        {
            if (stops.Count == 0)
                return new OrderedRoute(stops, false, null);

            // 1. Split into Europe and Anatolia
            var europeStops = stops.Where(s => IsEurope(s)).ToList();
            var anatoliaStops = stops.Where(s => !IsEurope(s)).ToList();

            bool crossesBosphorus = europeStops.Count > 0 && anatoliaStops.Count > 0;

            // 2. Determine start side
            bool startIsEurope = true; // default
            if (startLatitude.HasValue && startLongitude.HasValue)
                startIsEurope = startLongitude.Value < BosphorusLongitude;

            // 3. Sort each side: DeliveryWindowStart asc (null last), then farthest-first within same window
            var sortedEurope = SortSide(europeStops, startLatitude, startLongitude, startIsEurope ? null : GetSideCenter(anatoliaStops));
            var sortedAnatolia = SortSide(anatoliaStops, startLatitude, startLongitude, !startIsEurope ? null : GetSideCenter(europeStops));

            // 4. Combine: start side first → [bridge] → other side
            var result = new List<StopInfo>();
            string? bridgeNotice = null;
            bool hasBridge = false;

            if (!crossesBosphorus)
            {
                // Single side — no bridge
                result.AddRange(startIsEurope ? sortedEurope : sortedAnatolia);
            }
            else
            {
                var firstSide  = startIsEurope ? sortedEurope   : sortedAnatolia;
                var secondSide = startIsEurope ? sortedAnatolia : sortedEurope;

                result.AddRange(firstSide);

                // Bridge waypoint
                if (!string.IsNullOrWhiteSpace(vehicleType) &&
                    BridgeWaypoints.TryGetValue(vehicleType, out var bridge))
                {
                    result.Add(new StopInfo(bridge.Code, bridge.Name, bridge.Address, null, null, null, null));
                    bridgeNotice = bridge.Name;
                    hasBridge = true;
                }

                result.AddRange(secondSide);
            }

            // 5. ReturnToStart — add start coords as last stop marker (handled by caller)
            // We just return the ordered list; the caller appends the return destination.

            return new OrderedRoute(result, hasBridge, bridgeNotice);
        }

        private static bool IsEurope(StopInfo stop)
        {
            // Coordinate-based check (primary)
            if (stop.Longitude.HasValue)
                return stop.Longitude.Value < BosphorusLongitude;

            // Address-based fallback
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
                    if (upper.Contains(kwNorm)) return false; // Anatolia
                }
            }

            return true; // Default: Europe
        }

        private static List<StopInfo> SortSide(
            List<StopInfo> stops,
            double? refLat,
            double? refLon,
            (double Lat, double Lon)? altRef)
        {
            if (stops.Count == 0) return stops;

            double startLat = refLat ?? altRef?.Lat ?? stops[0].Latitude ?? 41.0;
            double startLon = refLon ?? altRef?.Lon ?? stops[0].Longitude ?? 28.9;

            var result = new List<StopInfo>();
            double curLat = startLat;
            double curLon = startLon;

            // Windowed stops: group by DeliveryWindowStart ascending, nearest-neighbor within each group
            var windowGroups = stops
                .Where(s => s.DeliveryWindowStart.HasValue)
                .GroupBy(s => s.DeliveryWindowStart!.Value)
                .OrderBy(g => g.Key);

            foreach (var group in windowGroups)
            {
                (var ordered, curLat, curLon) = NearestNeighborChain(group.ToList(), curLat, curLon);
                result.AddRange(ordered);
            }

            // Windowless stops: single nearest-neighbor chain continuing from last position
            var windowless = stops.Where(s => !s.DeliveryWindowStart.HasValue).ToList();
            if (windowless.Count > 0)
            {
                (var ordered, _, _) = NearestNeighborChain(windowless, curLat, curLon);
                result.AddRange(ordered);
            }

            return result;
        }

        /// <summary>
        /// Nearest-neighbor chain starting from the FARTHEST stop.
        /// First stop: farthest from current position (reference point).
        /// Subsequent stops: nearest unvisited from the last visited stop.
        /// Returns ordered stops and the exit coordinates (last stop's position).
        /// </summary>
        private static (List<StopInfo> Ordered, double ExitLat, double ExitLon) NearestNeighborChain(
            List<StopInfo> stops, double curLat, double curLon)
        {
            var remaining = new List<StopInfo>(stops);
            var ordered = new List<StopInfo>(stops.Count);

            // Start from the farthest stop
            var first = remaining
                .OrderByDescending(s => Haversine(curLat, curLon,
                    s.Latitude  ?? curLat,
                    s.Longitude ?? curLon))
                .First();

            ordered.Add(first);
            remaining.Remove(first);
            curLat = first.Latitude  ?? curLat;
            curLon = first.Longitude ?? curLon;

            // Continue with nearest-neighbor
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
