using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Zones.Commands.CreateZone
{
    public record CreateZoneCommand : IRequest<int>
    {
        public string Name { get; init; } = "";
        public int Order { get; init; }
        public bool IsOutOfCity { get; init; } = false;
    }

    public class CreateZoneCommandHandler : IRequestHandler<CreateZoneCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateZoneCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateZoneCommand request, CancellationToken cancellationToken)
        {
            var entity = new Zone
            {
                Name = request.Name,
                Order = request.Order,
                IsOutOfCity = request.IsOutOfCity
            };

            _context.Zones.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
