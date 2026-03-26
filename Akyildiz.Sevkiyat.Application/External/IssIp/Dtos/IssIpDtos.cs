using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Akyildiz.Sevkiyat.Application.External.IssIp.Dtos
{
    public sealed class IssSiparisHeaderDto
    {
        public int SiparisID { get; init; }
        public string? SiparisNo { get; init; }
        public string? TalepNo { get; init; }
        public string? Tarih { get; init; } // Changed to string for dd.MM.yyyy
        public string? KurumKodu { get; init; }
        public string? ProjeKodu { get; init; }
        public string? TeslimTarihi { get; init; } // Changed to string for dd.MM.yyyy
        public string? TeslimAlacakKisiler { get; init; }
        public string? TeslimAlacakTelefonNumaralari { get; init; }
        public string? TalepTuru { get; init; }
        public string? Aciklama { get; init; }
        public string? Donem { get; init; }
        public string? YoneticiMailAdresleri { get; init; }
    }

    public sealed class IssSiparisLineDto
    {
        public int SiparisID { get; init; }
        public string? MalzemeKodu { get; init; }
        public string? MalzemeAdi { get; init; }
        public decimal? Miktari { get; init; }
        public string? Birimi { get; init; }
        public decimal? ListeFiyati { get; init; }
        public decimal? Iskonto { get; init; }
        public decimal? BirimFiyati { get; init; }
        public decimal? KDVOrani { get; init; }
    }

    public sealed class IssSiparisDetailDto
    {
        public IssSiparisHeaderDto? Header { get; set; }
        public List<IssSiparisLineDto> Lines { get; set; } = new();
    }

    public sealed class IssProjeDto
    {
        public string? ProjeKodu { get; init; }
        public string? ProjeAdi { get; init; }
        public string? KurumKodu { get; init; }
        public string? MalzemeTeslimAdresi { get; init; }
    }

    public sealed class IssMalzemeDto
    {
        public string? MalzemeKodu { get; init; }
        public string? MalzemeAdi { get; init; }
        public string? Birimi { get; init; }
    }

    public sealed class ISSIpEnvelope
    {
        public JsonElement Root { get; init; }
    }
}
