using System.Net;
using System.Net.Http;
using System.Text.Json;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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
                    SafeLog(LogLevel.Warning, exception, "Validation error occurred");
                    break;

                case DomainException domainException:
                    code = StatusCodes.Status400BadRequest;
                    type = "business_rule_violation";
                    message = domainException.Message;
                    SafeLog(LogLevel.Warning, exception, "Domain/Business error occurred");
                    break;

                case NotFoundException notFoundException:
                    code = StatusCodes.Status404NotFound;
                    type = "not_found";
                    message = notFoundException.Message;
                    SafeLog(LogLevel.Warning, exception, "Resource not found");
                    break;

                case ConflictException conflictException:
                    code = StatusCodes.Status409Conflict;
                    type = "conflict";
                    message = conflictException.Message;
                    SafeLog(LogLevel.Warning, exception, "Conflict occurred");
                    break;

                case DbUpdateConcurrencyException:
                    code = StatusCodes.Status409Conflict;
                    type = "concurrency_conflict";
                    message = "Bu kayıt başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.";
                    SafeLog(LogLevel.Warning, exception, "Optimistic concurrency conflict occurred");
                    break;

                case UnauthorizedException unauthorizedException:
                    code = StatusCodes.Status401Unauthorized;
                    type = "unauthorized";
                    message = unauthorizedException.Message;
                    SafeLog(LogLevel.Warning, exception, "Unauthorized access attempt");
                    break;

                case ForbiddenException forbiddenException:
                    code = StatusCodes.Status403Forbidden;
                    type = "forbidden";
                    message = forbiddenException.Message;
                    SafeLog(LogLevel.Warning, exception, "Forbidden action attempt");
                    break;

                case HttpRequestException httpEx:
                    code = StatusCodes.Status502BadGateway;
                    type = "external_service_error";
                    message = $"Harici servis hatası: {httpEx.Message}";
                    SafeLog(LogLevel.Error, exception, "External HTTP service error");
                    break;

                case OperationCanceledException when !context.RequestAborted.IsCancellationRequested:
                    code = StatusCodes.Status504GatewayTimeout;
                    type = "external_service_timeout";
                    message = "Harici servise bağlantı zaman aşımına uğradı. Netsis sunucusuna erişilemiyor olabilir.";
                    SafeLog(LogLevel.Error, exception, "External service timeout (Netsis unreachable?)");
                    break;

                default:
                    SafeLog(LogLevel.Error, exception, $"Unhandled exception occurred. TraceId={context.TraceIdentifier}");
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

        // Linux'ta bazı native stack frame'leri BadImageFormatException fırlatabilir;
        // loglama başarısız olsa bile HTTP yanıtının dönmesini garanti altına alır.
        private void SafeLog(LogLevel level, Exception exception, string messageTemplate)
        {
            try
            {
                _logger.Log(level, exception, messageTemplate);
            }
            catch
            {
                try
                {
                    _logger.Log(level, "{Message} | {ExceptionType}: {ExceptionMessage}",
                        messageTemplate, exception.GetType().Name, exception.Message);
                }
                catch { /* loglama tamamen başarısız, yanıtı engelleme */ }
            }
        }
}
}
