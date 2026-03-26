using System.Text.Json.Serialization;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.IssIp
{
    public sealed class ISSIpRequest<TParams>
    {
        [JsonPropertyName("MethodName")]
        public required string MethodName { get; init; }
        
        [JsonPropertyName("MtdParams")]
        public required TParams MtdParams { get; init; }
    }

    public class AuthParams
    {
        [JsonPropertyName("KullaniciAdi")]
        public required string KullaniciAdi { get; init; }
        
        [JsonPropertyName("Sifre")]
        public required string Sifre { get; init; }
    }

    public sealed class GetSiparisListesiParams : AuthParams
    {
        [JsonPropertyName("BaslangicTarihi")]
        public required string BaslangicTarihi { get; init; } // "yyyy-MM-dd"
        
        [JsonPropertyName("BitisTarihi")]
        public required string BitisTarihi { get; init; }     // "yyyy-MM-dd"
    }

    public sealed class GetSiparisParams : AuthParams
    {
        [JsonPropertyName("SiparisNo")]
        public required string SiparisNo { get; init; }
    }

    public sealed class GetProjeParams : AuthParams
    {
        [JsonPropertyName("ProjeKodu")]
        public string? ProjeKodu { get; init; }
    }

    public sealed class GetMalzemeParams : AuthParams
    {
        [JsonPropertyName("MalzemeKodu")]
        public string? MalzemeKodu { get; init; }
    }
}
