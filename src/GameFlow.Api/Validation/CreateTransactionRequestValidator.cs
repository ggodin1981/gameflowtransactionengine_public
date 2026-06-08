using FluentValidation;
using GameFlow.Shared.Contracts.Transactions;

namespace GameFlow.Api.Validation;

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.PlayerExternalId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PlayerUsername).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Country).NotEmpty().Length(2, 3);
        RuleFor(x => x.GameExternalId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.GameName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
