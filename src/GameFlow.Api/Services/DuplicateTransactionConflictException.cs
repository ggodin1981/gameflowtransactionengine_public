namespace GameFlow.Api.Services;

public sealed class DuplicateTransactionConflictException(string message) : Exception(message);
