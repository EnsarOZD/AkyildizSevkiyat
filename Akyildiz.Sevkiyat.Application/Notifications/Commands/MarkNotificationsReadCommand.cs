using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Notifications.Commands;

public record MarkNotificationsReadCommand(int? NotificationId = null)
    : IRequest, IRequireRoles
{
    public IReadOnlyList<string> AllowedRoles =>
        ["Admin", "Manager", "Accounting", "Warehouse", "Driver", "Dispatcher"];
}

public class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationsReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkNotificationsReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var query = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead);

        if (request.NotificationId.HasValue)
            query = query.Where(n => n.Id == request.NotificationId.Value);

        await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
