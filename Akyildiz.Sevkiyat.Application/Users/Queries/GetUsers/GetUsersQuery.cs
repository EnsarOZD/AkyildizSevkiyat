using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Users.Queries.GetUsers
{
    public record GetUsersQuery : IRequest<List<UserListDto>>, IRequireRoles
    {
        public IReadOnlyList<string> AllowedRoles => new[] { "Admin", "Manager" };
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserListDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetUsersQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            return await _context.Users
                .OrderBy(u => u.FirstName)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
