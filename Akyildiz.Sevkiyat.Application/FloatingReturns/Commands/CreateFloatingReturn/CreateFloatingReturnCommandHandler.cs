using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.FloatingReturns.Commands.CreateFloatingReturn
{
    public class CreateFloatingReturnCommandHandler : IRequestHandler<CreateFloatingReturnCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateFloatingReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(CreateFloatingReturnCommand request, CancellationToken cancellationToken)
        {
            // Either StockMasterId or free-text stock info must be provided
            if (!request.StockMasterId.HasValue &&
                string.IsNullOrWhiteSpace(request.StockCodeFree) &&
                string.IsNullOrWhiteSpace(request.StockNameFree))
            {
                throw new DomainException("Stok bilgisi girilmelidir: Stok kartı seçin veya serbest stok kodu/adı girin.");
            }

            // Validate StockMasterId if provided
            if (request.StockMasterId.HasValue)
            {
                var exists = await _context.StockMasters
                    .AnyAsync(s => s.Id == request.StockMasterId.Value, cancellationToken);
                if (!exists)
                    throw new NotFoundException("StockMaster", request.StockMasterId.Value);
            }

            var entity = new FloatingReturn
            {
                ReturnDate = request.ReturnDate,
                StockMasterId = request.StockMasterId,
                StockCodeFree = request.StockCodeFree,
                StockNameFree = request.StockNameFree,
                Qty = request.Qty,
                ReturnReason = request.ReturnReason,
                Note = request.Note,
                CreatedByUserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FloatingReturns.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
    }
}
