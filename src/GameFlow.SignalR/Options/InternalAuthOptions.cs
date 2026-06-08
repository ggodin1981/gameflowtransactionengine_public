namespace GameFlow.SignalR.Options;

public sealed class InternalAuthOptions
{
    public const string SectionName = "InternalAuth";

    public string ApiKey { get; set; } = "local-dev-key";
}
