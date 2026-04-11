using Akyildiz.Sevkiyat.Application.Common.Validators;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Users.Commands.ResetPassword
{
    public record ResetPasswordCommand(int UserId, string NewPassword) : IRequest;

    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.NewPassword).MustBeValidPassword();
        }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                ?? throw new NotFoundException($"Kullanıcı bulunamadı (Id: {request.UserId}).");

            var hash = _passwordHasher.CreateHash(request.NewPassword, out string salt);
            user.ResetPassword(hash, salt);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
