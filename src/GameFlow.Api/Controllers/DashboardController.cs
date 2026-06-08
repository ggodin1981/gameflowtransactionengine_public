using GameFlow.Api.Services;
using GameFlow.Shared.Contracts.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardQueryService dashboardQueryService) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var response = await dashboardQueryService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }
}
