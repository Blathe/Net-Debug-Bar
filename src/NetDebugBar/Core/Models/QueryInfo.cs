namespace NetDebugBar.Core.Models;

public class QueryInfo
{
    public string Sql { get; set; } = "";
    public double Duration { get; set; }
    public long SizeBytes { get; set; }
    public List<QueryParameterInfo> Parameters { get; set; } = new();
}