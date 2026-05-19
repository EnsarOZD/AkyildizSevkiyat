using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Common.Validators;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Users.Commands.CreateUser
{
    public record CreateUserCommand(
        string Email,
        string FirstName,
        string LastName,
        string Password,
        UserRole Role,
        string? Username = null
    ) : IRequest<int>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles =>
            new[] { "Admin" };
    }

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Password).MustBeValidPassword();
            RuleFor(x => x.Username).MaximumLength(100).When(x => x.Username != null);
        }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
                throw new ConflictException($"'{request.Email}' e-posta adresi zaten kullanılıyor.");

            var username = string.IsNullOrWhiteSpace(request.Username) ? request.Email : request.Username;

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == username, cancellationToken);

            if (usernameExists)
                throw new ConflictException($"'{username}' kullanıcı adı zaten kullanılıyor.");

            var hash = _passwordHasher.CreateHash(request.Password, out string salt);

            var user = User.Create(request.Email, request.FirstName, request.LastName, hash, salt, request.Role, username);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
