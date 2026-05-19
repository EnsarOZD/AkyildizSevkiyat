using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.StockConsumptions.Commands.CreateStockConsumption
{
    public record CreateStockConsumptionCommand : IRequest<int>
    {
        public int StockMasterId { get; init; }
        public StockConsumptionType Type { get; init; }
        public decimal Quantity { get; init; }
        public DateOnly Date { get; init; }
        public string? Reason { get; init; }
        public string? RecipientName { get; init; }
        public decimal? SalePrice { get; init; }
        public string? Note { get; init; }
    }

    public class CreateStockConsumptionCommandValidator : AbstractValidator<CreateStockConsumptionCommand>
    {
        public CreateStockConsumptionCommandValidator()
        {
            RuleFor(x => x.StockMasterId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");
            RuleFor(x => x.Date).NotEmpty();

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Zai kaydı için sebep girilmesi zorunludur.")
                .When(x => x.Type == StockConsumptionType.Zai);

            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Dahili kullanım için teslim alan kişi girilmesi zorunludur.")
                .When(x => x.Type == StockConsumptionType.DahiliKullanim);

            RuleFor(x => x.SalePrice)
                .NotNull().WithMessage("Depo satışı için satış fiyatı girilmesi zorunludur.")
                .GreaterThan(0).WithMessage("Satış fiyatı sıfırdan büyük olmalıdır.")
                .When(x => x.Type == StockConsumptionType.DepoSatisi);
        }
    }

    public class CreateStockConsumptionCommandHandler : IRequestHandler<CreateStockConsumptionCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateStockConsumptionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<int> Handle(CreateStockConsumptionCommand request, CancellationToken cancellationToken)
        {
            var stock = await _context.StockMasters
                .FirstOrDefaultAsync(s => s.Id == request.StockMasterId, cancellationToken)
                ?? throw new NotFoundException("StockMaster", request.StockMasterId);

            var entity = new StockConsumption
            {
                StockMasterId = stock.Id,
                StockCodeSnapshot = stock.StockCode,
                StockNameSnapshot = stock.StockName,
                UnitSnapshot = stock.Unit,
                Type = request.Type,
                Quantity = request.Quantity,
                Date = request.Date,
                Reason = request.Reason,
                RecipientName = request.RecipientName,
                SalePrice = request.SalePrice,
                Note = request.Note,
                CreatedBy = _currentUserService.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockConsumptions.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
