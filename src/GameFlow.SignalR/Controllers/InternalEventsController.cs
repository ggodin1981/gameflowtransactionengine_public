using GameFlow.SignalR.Hubs;
using GameFlow.SignalR.Options;
using GameFlow.SignalR.Services;
using GameFlow.Shared.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace GameFlow.SignalR.Controllers;

[ApiController]
[Route("internal")]
public sealed class InternalEventsController(
    IHubContext<TransactionHub> hubContext,
    ConnectionRegistry connectionRegistry,
    IOptions<InternalAuthOptions> authOptions) : ControllerBase
{
    private readonly InternalAuthOptions _authOptions = authOptions.Value;

    [HttpPost("events/transactions")]
    public async Task<ActionResult<object>> PublishTransactionEventAsync(
        [FromBody] TransactionLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("X-GameFlow-Key", out var apiKey) || apiKey != _authOptions.ApiKey)
        {
            return Unauthorized();
        }

        await hubContext.Clients.All.SendAsync("transaction-updated", lifecycleEvent, cancellationToken);
        return Ok(new
        {
            broadcast = true,
            activeConnections = connectionRegistry.ActiveConnections
        });
    }

    [HttpGet("stats")]
    public ActionResult<object> GetStats()
    {
        return Ok(new
        {
            activeConnections = connectionRegistry.ActiveConnections
        });
    }
}
