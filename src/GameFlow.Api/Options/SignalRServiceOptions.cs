namespace GameFlow.Api.Options;

public sealed class SignalRServiceOptions
{
    public const string SectionName = "SignalRService";

    public string BaseUrl { get; set; } = "http://localhost:5053/";
    public string ApiKey { get; set; } = "local-dev-key";
}
