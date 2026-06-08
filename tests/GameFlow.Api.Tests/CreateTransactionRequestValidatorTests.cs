using GameFlow.Api.Validation;
using GameFlow.Shared.Contracts.Transactions;
using GameFlow.Shared.Enums;

namespace GameFlow.Api.Tests;

public sealed class CreateTransactionRequestValidatorTests
{
    [Fact]
    public void Rejects_zero_or_negative_amounts()
    {
        var validator = new CreateTransactionRequestValidator();
        var request = new CreateTransactionRequest
        {
            PlayerExternalId = "PLY-1",
            PlayerUsername = "tester",
            Country = "PH",
            Currency = "EUR",
            GameExternalId = "GM-1",
            GameName = "Book of Nile",
            Provider = "EveryMatrix Studio",
            Amount = 0m,
            Type = TransactionType.Debit
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTransactionRequest.Amount));
    }
}
