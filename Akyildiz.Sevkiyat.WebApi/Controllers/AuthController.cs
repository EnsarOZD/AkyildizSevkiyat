using Akyildiz.Sevkiyat.Application.Auth.Commands.Login;
using Akyildiz.Sevkiyat.Application.Auth.Commands.Logout;
using Akyildiz.Sevkiyat.Application.Auth.Commands.RefreshToken;
using Akyildiz.Sevkiyat.WebApi.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Akyildiz.Sevkiyat.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoginAttemptTracker _loginTracker;
        private readonly IWebHostEnvironment _env;

        public AuthController(IMediator mediator, ILoginAttemptTracker loginTracker, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _loginTracker = loginTracker;
            _env = env;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (_loginTracker.IsBlocked(ip))
            {
                return StatusCode(429, new
                {
                    type = "rate_limit_exceeded",
                    message = "Çok fazla giriş denemesi yapıldı. 5 dakika sonra tekrar deneyin."
                });
            }

            _loginTracker.RecordAttempt(ip);

            var result = await _mediator.Send(command);

            // #1: Set HttpOnly cookies for browser clients
            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(result); // Tokens still in body for Postman/API clients
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-refresh")] // #9
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
        {
            // #1: Read refresh token from cookie first, fall back to request body (Postman compat)
            var refreshToken = Request.Cookies.TryGetValue("sevkiyat_rt", out var cookieRt) && !string.IsNullOrEmpty(cookieRt)
                ? cookieRt
                : request?.RefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { type = "unauthorized", message = "Refresh token bulunamadı." });

            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

            // #1: Issue new cookies
            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
        {
            // #1: Read refresh token from cookie or body
            var refreshToken = Request.Cookies.TryGetValue("sevkiyat_rt", out var cookieRt) && !string.IsNullOrEmpty(cookieRt)
                ? cookieRt
                : request?.RefreshToken ?? string.Empty;

            await _mediator.Send(new LogoutCommand(refreshToken), cancellationToken);

            // #1: Clear auth cookies
            ClearAuthCookies();

            return NoContent();
        }

        [HttpGet("login-blocks")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetLoginBlocks()
        {
            var list = _loginTracker.GetBlockedList()
                .Select(b => new
                {
                    ip = b.Ip,
                    attempts = b.Attempts,
                    blockedUntil = b.BlockedUntil
                });
            return Ok(list);
        }

        [HttpPost("reset-login-block")]
        [Authorize(Roles = "Admin")]
        public IActionResult ResetLoginBlock([FromBody] ResetLoginBlockRequest request)
        {
            _loginTracker.Reset(request.Ip);
            return NoContent();
        }

        // ── Cookie Helpers ────────────────────────────────────────────────────

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            var isProd = !_env.IsDevelopment();
            var baseOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,                  // HTTPS-only in production
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
            };

            // Access token: session cookie (expires with browser session)
            Response.Cookies.Append("sevkiyat_jwt", accessToken, new CookieOptions
            {
                HttpOnly = baseOptions.HttpOnly,
                Secure = baseOptions.Secure,
                SameSite = baseOptions.SameSite,
                IsEssential = baseOptions.IsEssential,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60), // Match JWT expiry
            });

            // Refresh token: longer-lived persistent cookie
            Response.Cookies.Append("sevkiyat_rt", refreshToken, new CookieOptions
            {
                HttpOnly = baseOptions.HttpOnly,
                Secure = baseOptions.Secure,
                SameSite = baseOptions.SameSite,
                IsEssential = baseOptions.IsEssential,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            });
        }

        private void ClearAuthCookies()
        {
            var isProd = !_env.IsDevelopment();
            var clearOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UnixEpoch,
            };
            Response.Cookies.Append("sevkiyat_jwt", "", clearOptions);
            Response.Cookies.Append("sevkiyat_rt", "", clearOptions);
        }
    }

    public record RefreshTokenRequest(string? RefreshToken);
    public record LogoutRequest(string? RefreshToken);
    public record ResetLoginBlockRequest(string Ip);
}
