using GameFlow.Api.Services;
using GameFlow.Shared.Contracts.Players;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController(IPlayerProfileService playerProfileService) : ControllerBase
{
    [HttpGet("{externalPlayerId}")]
    [ProducesResponseType(typeof(PlayerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileResponse>> GetAsync(string externalPlayerId, CancellationToken cancellationToken)
    {
        var player = await playerProfileService.GetByExternalIdAsync(externalPlayerId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }
}
