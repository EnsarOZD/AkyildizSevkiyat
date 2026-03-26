using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Application.External.IssIp.Dtos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Interfaces
{
    public interface IISSIpClient
    {
        Task<ISSIpEnvelope> GetSiparisListesiAsync(DateTime start, DateTime end, CancellationToken ct);
        Task<ISSIpEnvelope> GetSiparisAsync(string siparisNo, CancellationToken ct);
        Task<ISSIpEnvelope> GetProjeAsync(string? projeKodu, CancellationToken ct);
        Task<ISSIpEnvelope> GetMalzemeAsync(string? malzemeKodu, CancellationToken ct);
    }
}
