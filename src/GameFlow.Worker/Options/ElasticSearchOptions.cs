namespace GameFlow.Worker.Options;

public sealed class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

    public string BaseUrl { get; set; } = "http://localhost:9200/";
    public string IndexName { get; set; } = "gameflow-transactions";
}
