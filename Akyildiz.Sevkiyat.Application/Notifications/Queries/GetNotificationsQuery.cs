using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Notifications.Queries;

public record NotificationDto(
    int Id,
    string Title,
    string Body,
    string? Url,
    string EventType,
    bool IsRead,
    DateTime CreatedAt);

public record GetNotificationsQuery(int Page = 1, int PageSize = 30)
    : IRequest<List<NotificationDto>>, IRequireRoles
{
    public IReadOnlyList<string> AllowedRoles =>
        ["Admin", "Manager", "Accounting", "Warehouse", "Driver", "Dispatcher"];
}

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Url, n.EventType, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }
}
