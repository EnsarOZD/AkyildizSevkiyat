using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Application.Notifications.Commands;
using Akyildiz.Sevkiyat.Application.Notifications.Queries;
using Akyildiz.Sevkiyat.Infrastructure.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Akyildiz.Sevkiyat.WebApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly SseChannelManager _sse;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(IMediator mediator, SseChannelManager sse, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _sse = sse;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(page), ct);
        return Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _mediator.Send(new MarkNotificationsReadCommand(), ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _mediator.Send(new MarkNotificationsReadCommand(id), ct);
        return NoContent();
    }

    [HttpPost("push-subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request, CancellationToken ct)
    {
        await _mediator.Send(new SavePushSubscriptionCommand(request.Endpoint, request.P256DH, request.Auth), ct);
        return NoContent();
    }

    [HttpDelete("push-subscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken ct)
    {
        await _mediator.Send(new DeletePushSubscriptionCommand(request.Endpoint), ct);
        return NoContent();
    }

    /// <summary>
    /// SSE stream — client bağlanır ve gerçek zamanlı bildirim alır.
    /// </summary>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            Response.StatusCode = 401;
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no"); // nginx: disable buffering

        // Heartbeat to keep connection alive (every 25s)
        using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        var channel = _sse.Subscribe(userId.Value);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                while (await heartbeatTimer.WaitForNextTickAsync(linked.Token))
                {
                    await Response.WriteAsync(": heartbeat\n\n", linked.Token);
                    await Response.Body.FlushAsync(linked.Token);
                }
            }
            catch { /* client disconnected */ }
        }, linked.Token);

        try
        {
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
            linked.Cancel();
            _sse.Unsubscribe(userId.Value, channel);
        }
    }
}

public record PushSubscribeRequest(string Endpoint, string P256DH, string Auth);
public record PushUnsubscribeRequest(string Endpoint);
