using GameFlow.Api.Models;
using GameFlow.Api.Services;
using GameFlow.Shared.Contracts.Transactions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(
    ITransactionCommandService transactionCommandService,
    ITransactionQueryService transactionQueryService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transaction-write")]
    [ProducesResponseType(typeof(TransactionAcceptedResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TransactionAcceptedResponse>> CreateAsync(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await transactionCommandService.CreateAsync(request, cancellationToken);
        return Accepted($"/api/transactions/{response.ExternalTransactionId}", response);
    }

    [HttpGet("{externalTransactionId}")]
    [ProducesResponseType(typeof(TransactionSearchItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionSearchItem>> GetByIdAsync(string externalTransactionId, CancellationToken cancellationToken)
    {
        var transaction = await transactionQueryService.GetByExternalTransactionIdAsync(externalTransactionId, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSearchItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionSearchItem>>> SearchAsync(
        [FromQuery] TransactionSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var results = await transactionQueryService.SearchAsync(criteria, cancellationToken);
        return Ok(results);
    }
}
