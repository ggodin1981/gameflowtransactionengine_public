using System.Text.Json;
using GameFlow.Shared.Contracts.Players;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GameFlow.Api.Services;

public sealed class PlayerProfileService(
    GameFlowDbContext dbContext,
    IDistributedCache cache,
    ILogger<PlayerProfileService> logger) : IPlayerProfileService
{
    public async Task<PlayerProfileResponse?> GetByExternalIdAsync(string externalPlayerId, CancellationToken cancellationToken)
    {
        var cacheKey = $"player-profile:{externalPlayerId}";
        var cachedPayload = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(cachedPayload))
        {
            logger.LogDebug("Player lookup cache hit for {ExternalPlayerId}.", externalPlayerId);
            return JsonSerializer.Deserialize<PlayerProfileResponse>(cachedPayload);
        }

        var player = await dbContext.Players
            .AsNoTracking()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.ExternalPlayerId == externalPlayerId, cancellationToken);

        if (player is null)
        {
            return null;
        }

        var response = new PlayerProfileResponse
        {
            PlayerId = player.Id,
            ExternalPlayerId = player.ExternalPlayerId,
            Username = player.Username,
            Country = player.Country,
            Currency = player.Currency,
            TotalTransactions = player.Transactions.Count,
            LifetimeVolume = player.Transactions.Sum(x => x.Amount),
            LastActivityUtc = player.Transactions
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => x.CreatedAtUtc)
                .FirstOrDefault()
        };

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            cancellationToken);

        return response;
    }
}
