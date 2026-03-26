using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Akyildiz.Sevkiyat.Application.Transport.Drivers.Commands.DeleteDriver
{
    public record DeleteDriverCommand(int Id) : IRequest<bool>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin" };
    }

    public class DeleteDriverCommandHandler : IRequestHandler<DeleteDriverCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public DeleteDriverCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<bool> Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
        {
            var driver = await _context.Drivers.FindAsync(new object[] { request.Id }, cancellationToken);
            if (driver == null) return false;

            // Soft Delete
            driver.IsActive = false;
            
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
