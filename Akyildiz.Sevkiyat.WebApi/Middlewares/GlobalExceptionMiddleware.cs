using System.Net;
using System.Text.Json;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;

namespace Akyildiz.Sevkiyat.WebApi.Middlewares
{
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = StatusCodes.Status500InternalServerError;
            var type = "server_error";
            var message = "Unexpected error occurred.";
            object? errors = null;

            switch (exception)
            {
                case ValidationException validationException:
                    code = StatusCodes.Status400BadRequest;
                    type = "validation_error";
                    message = "One or more validation errors occurred.";
                    errors = validationException.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    });
                    _logger.LogWarning(exception, "Validation error occurred");
                    break;

                case DomainException domainException:
                    code = StatusCodes.Status400BadRequest;
                    type = "business_rule_violation";
                    message = domainException.Message;
                    _logger.LogWarning(exception, "Domain/Business error occurred");
                    break;

                case NotFoundException notFoundException:
                    code = StatusCodes.Status404NotFound;
                    type = "not_found";
                    message = notFoundException.Message;
                    _logger.LogWarning(exception, "Resource not found");
                    break;

                case ConflictException conflictException:
                    code = StatusCodes.Status409Conflict;
                    type = "conflict";
                    message = conflictException.Message;
                    _logger.LogWarning(exception, "Conflict occurred");
                    break;

                case UnauthorizedException unauthorizedException:
                    code = StatusCodes.Status401Unauthorized;
                    type = "unauthorized";
                    message = unauthorizedException.Message;
                    _logger.LogWarning(exception, "Unauthorized access attempt");
                    break;

                case ForbiddenException forbiddenException:
                    code = StatusCodes.Status403Forbidden;
                    type = "forbidden";
                    message = forbiddenException.Message;
                    _logger.LogWarning(exception, "Forbidden action attempt");
                    break;

                default:
                    _logger.LogError(exception, "Unhandled exception occurred. TraceId={TraceId}", context.TraceIdentifier);
                    message = "Beklenmeyen bir hata oluştu.";
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;

            var payload = new
            {
                type,
                message,
                errors,
                traceId = context.TraceIdentifier
            };

            var result = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await context.Response.WriteAsync(result);
        }
}
}
