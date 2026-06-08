using GameFlow.Api.Services;
using GameFlow.Shared.Contracts.Audit;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController(ITransactionQueryService transactionQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var logs = await transactionQueryService.GetAuditLogsAsync(cancellationToken);
        return Ok(logs);
    }
}
