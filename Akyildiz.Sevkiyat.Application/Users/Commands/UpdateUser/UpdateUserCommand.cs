using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Users.Commands.UpdateUser
{
    public record UpdateUserCommand(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        UserRole Role,
        string? Username = null
    ) : IRequest, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin" };
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Username).MaximumLength(100).When(x => x.Username != null);
        }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateUserCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException($"Kullanıcı bulunamadı (Id: {request.Id}).");

            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.Id, cancellationToken);

            if (emailTaken)
                throw new ConflictException($"'{request.Email}' e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var usernameTaken = await _context.Users
                    .AnyAsync(u => u.Username == request.Username && u.Id != request.Id, cancellationToken);

                if (usernameTaken)
                    throw new ConflictException($"'{request.Username}' kullanıcı adı başka bir kullanıcı tarafından kullanılıyor.");
            }

            user.UpdateProfile(request.Email, request.FirstName, request.LastName, request.Username);
            user.UpdateRole(request.Role);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
