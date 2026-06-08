using System.Linq.Expressions;
using GameFlow.Api.Models;
using GameFlow.Shared.Contracts.Audit;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Entities;
using GameFlow.Shared.Enums;
using GameFlow.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Api.Services;

public sealed class TransactionQueryService(GameFlowDbContext dbContext) : ITransactionQueryService
{
    public async Task<TransactionSearchItem?> GetByExternalTransactionIdAsync(string externalTransactionId, CancellationToken cancellationToken)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Player)
            .Include(x => x.Game)
            .Where(x => x.ExternalTransactionId == externalTransactionId)
            .Select(MapToSearchItem())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionSearchItem>> SearchAsync(TransactionSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Player)
            .Include(x => x.Game)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Player))
        {
            var player = criteria.Player.Trim();
            query = query.Where(x => x.Player!.ExternalPlayerId.Contains(player) || x.Player.Username.Contains(player));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransactionId))
        {
            var transactionId = criteria.TransactionId.Trim();
            query = query.Where(x => x.ExternalTransactionId.Contains(transactionId) || x.CorrelationId.Contains(transactionId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Game))
        {
            var game = criteria.Game.Trim();
            query = query.Where(x => x.Game!.ExternalGameId.Contains(game) || x.Game.Name.Contains(game));
        }

        if (Enum.TryParse<TransactionStatus>(criteria.Status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (criteria.DateFromUtc is not null)
        {
            query = query.Where(x => x.CreatedAtUtc >= criteria.DateFromUtc.Value);
        }

        if (criteria.DateToUtc is not null)
        {
            query = query.Where(x => x.CreatedAtUtc <= criteria.DateToUtc.Value);
        }

        if (criteria.MinAmount is not null)
        {
            query = query.Where(x => x.Amount >= criteria.MinAmount.Value);
        }

        if (criteria.MaxAmount is not null)
        {
            query = query.Where(x => x.Amount <= criteria.MaxAmount.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(MapToSearchItem())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new AuditLogDto
            {
                AuditLogId = x.Id,
                Action = x.Action,
                Actor = x.Actor,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                DetailsJson = x.DetailsJson,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private static Expression<Func<Transaction, TransactionSearchItem>> MapToSearchItem()
    {
        return transaction => new TransactionSearchItem
        {
            TransactionId = transaction.Id,
            ExternalTransactionId = transaction.ExternalTransactionId,
            CorrelationId = transaction.CorrelationId,
            PlayerExternalId = transaction.Player!.ExternalPlayerId,
            PlayerUsername = transaction.Player.Username,
            GameExternalId = transaction.Game!.ExternalGameId,
            GameName = transaction.Game.Name,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Status = transaction.Status,
            Type = transaction.Type.ToString(),
            CreatedAtUtc = transaction.CreatedAtUtc,
            ProcessedAtUtc = transaction.ProcessedAtUtc,
            FailureReason = transaction.FailureReason
        };
    }
}
