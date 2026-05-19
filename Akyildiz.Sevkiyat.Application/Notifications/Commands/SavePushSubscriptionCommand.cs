using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.Notifications.Commands;

public record SavePushSubscriptionCommand(string Endpoint, string P256DH, string Auth)
    : IRequest, IRequireRoles
{
    public IReadOnlyList<string> AllowedRoles =>
        ["Admin", "Manager", "Accounting", "Warehouse", "Driver", "Dispatcher"];
}

public record DeletePushSubscriptionCommand(string Endpoint) : IRequest, IRequireRoles
{
    public IReadOnlyList<string> AllowedRoles =>
        ["Admin", "Manager", "Accounting", "Warehouse", "Driver", "Dispatcher"];
}

public class SavePushSubscriptionCommandHandler : IRequestHandler<SavePushSubscriptionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SavePushSubscriptionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SavePushSubscriptionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is null)
        {
            _context.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256DH = request.P256DH,
                Auth = request.Auth,
            });
            await _context.SaveChangesAsync(ct);
        }
    }
}

public class DeletePushSubscriptionCommandHandler : IRequestHandler<DeletePushSubscriptionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeletePushSubscriptionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePushSubscriptionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint)
            .ExecuteDeleteAsync(ct);
    }
}
