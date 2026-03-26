using Akyildiz.Sevkiyat.Application.Common.Interfaces;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Common.Behaviors
{
    public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequireRoles
    {
        private readonly ICurrentUserService _currentUserService;

        public AuthorizationBehavior(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId == null || _currentUserService.Role == null)
                throw new UnauthorizedException("Bu işlem için giriş yapmanız gerekmektedir.");

            var userRole = _currentUserService.Role.ToString();
            var allowed = request.AllowedRoles.Any(r =>
                string.Equals(r, userRole, StringComparison.OrdinalIgnoreCase));

            if (!allowed)
                throw new ForbiddenException("Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.");

            return await next();
        }
    }
}
