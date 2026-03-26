using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using DriverEntity = Akyildiz.Sevkiyat.Domain.Entities.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.CreateDriver
{
    public record CreateDriverCommand(string FullName, string? Phone) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager" };
    }

    public class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public CreateDriverCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
        {
            var driver = new DriverEntity
            {
                FullName = request.FullName,
                Phone = request.Phone,
                IsActive = true
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync(cancellationToken);
            return driver.Id;
        }
    }
}
