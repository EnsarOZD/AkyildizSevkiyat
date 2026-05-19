using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.WarehouseLocations.Commands.CreatePickingFace
{
    /// <summary>
    /// Toplama gözlerini toplu oluşturur. İki adres şeması:
    ///
    /// Palet: {KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}
    ///   Örn: 2K-001-00
    ///
    /// Koli / Kutu: {KoridorNo}{Taraf}-{ModulNo:D3}-00-{InnerLevel}{InnerPosition:D2}
    ///   Koli kayar raf — kat = harf (A=zemin, B=orta, C=üst), pozisyon = 01-06
    ///   Kutu — kol harfi (A, B …) ve göz numarası (01, 02 …)
    ///   Örn: 2K-001-00-A01, 2K-001-00-C06
    /// </summary>
    public record CreatePickingFaceCommand(
        int           KoridorNo,
        string        Taraf,
        int           ModulFrom,
        int           ModulTo,
        ContainerType ContainerType,
        // Palet / Koli — kat range (KatFrom == KatTo → single level)
        int           KatFrom          = 0,
        int           KatTo            = 0,
        // Kutu only
        List<string>? InnerLevels       = null,
        int?          PositionsPerLevel = null,
        string?       Description       = null
    ) : IRequest<CreatePickingFaceResult>;

    public record CreatePickingFaceResult(int Created, int Skipped);

    public class CreatePickingFaceCommandHandler
        : IRequestHandler<CreatePickingFaceCommand, CreatePickingFaceResult>
    {
        private readonly IApplicationDbContext _context;
        public CreatePickingFaceCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<CreatePickingFaceResult> Handle(
            CreatePickingFaceCommand request,
            CancellationToken cancellationToken)
        {
            var taraf = request.Taraf.Trim().ToUpperInvariant();

            var existingCodes = await _context.WarehouseLocations
                .Where(l => l.KoridorNo == request.KoridorNo && l.Taraf == taraf)
                .Select(l => l.Code)
                .ToHashSetAsync(cancellationToken);

            var toAdd = new List<WarehouseLocation>();

            if (request.ContainerType == ContainerType.Box || request.ContainerType == ContainerType.Case)
            {
                // Koli (kayar raf) ve Kutu: harf bazlı iç adres — Kat=0, InnerLevel=harf, InnerPosition=pozisyon
                var levels = (request.InnerLevels ?? [])
                    .Select(l => l.Trim().ToUpperInvariant())
                    .Where(l => l.Length > 0)
                    .Distinct()
                    .ToList();

                int posCount = request.PositionsPerLevel ?? 0;
                if (levels.Count == 0 || posCount < 1)
                    return new CreatePickingFaceResult(0, 0);

                for (int modul = request.ModulFrom; modul <= request.ModulTo; modul++)
                foreach (var level in levels)
                for (int pos = 1; pos <= posCount; pos++)
                {
                    var code = WarehouseLocation.BuildCode(request.KoridorNo, taraf, modul, 0, level, pos);
                    if (existingCodes.Contains(code)) continue;
                    toAdd.Add(new WarehouseLocation
                    {
                        Code          = code,
                        KoridorNo     = request.KoridorNo,
                        Taraf         = taraf,
                        ModulNo       = modul,
                        Kat           = 0,
                        InnerLevel    = level,
                        InnerPosition = pos,
                        LocationType  = LocationType.PickingFace,
                        ContainerType = request.ContainerType,
                        Description   = request.Description?.Trim(),
                        IsActive      = true,
                    });
                    existingCodes.Add(code);
                }
            }
            else
            {
                // Palet: modül × kat aralığı, iç adres yok
                int katFrom = request.KatFrom;
                int katTo   = Math.Max(request.KatTo, request.KatFrom);

                for (int modul = request.ModulFrom; modul <= request.ModulTo; modul++)
                for (int kat = katFrom; kat <= katTo; kat++)
                {
                    var code = WarehouseLocation.BuildCode(request.KoridorNo, taraf, modul, kat);
                    if (existingCodes.Contains(code)) continue;
                    toAdd.Add(new WarehouseLocation
                    {
                        Code          = code,
                        KoridorNo     = request.KoridorNo,
                        Taraf         = taraf,
                        ModulNo       = modul,
                        Kat           = kat,
                        LocationType  = LocationType.PickingFace,
                        ContainerType = ContainerType.Pallet,
                        Description   = request.Description?.Trim(),
                        IsActive      = true,
                    });
                    existingCodes.Add(code);
                }
            }

            if (toAdd.Count > 0)
            {
                _context.WarehouseLocations.AddRange(toAdd);
                await _context.SaveChangesAsync(cancellationToken);
            }

            int innerCount = (request.InnerLevels?.Count(l => l.Trim().Length > 0) ?? 0) * (request.PositionsPerLevel ?? 0);
            int katRange   = Math.Max(request.KatTo, request.KatFrom) - request.KatFrom + 1;
            int totalExpected = (request.ContainerType == ContainerType.Box || request.ContainerType == ContainerType.Case)
                ? (request.ModulTo - request.ModulFrom + 1) * innerCount
                : (request.ModulTo - request.ModulFrom + 1) * katRange;

            return new CreatePickingFaceResult(toAdd.Count, totalExpected - toAdd.Count);
        }
    }
}
