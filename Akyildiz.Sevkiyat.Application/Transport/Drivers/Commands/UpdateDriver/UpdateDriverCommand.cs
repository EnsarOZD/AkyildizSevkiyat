using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.UpdateDriver
{
    public record UpdateDriverCommand(int Id, string FullName, string? Phone, bool IsActive, int? UserId) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin", "Manager", "Accounting" };
    }

    public class UpdateDriverCommandHandler : IRequestHandler<UpdateDriverCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public UpdateDriverCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<bool> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers.FindAsync(new object[] { request.Id }, cancellationToken);
            if (driver == null) return false;

            driver.FullName = request.FullName;
            driver.Phone = request.Phone;
            driver.IsActive = request.IsActive;
            driver.UserId = request.UserId;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
