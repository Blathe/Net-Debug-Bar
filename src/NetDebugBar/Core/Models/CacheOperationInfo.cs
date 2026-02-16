namespace NetDebugBar.Core.Models;

public class CacheOperationInfo
{
    public string Key { get; set; } = "";
    public CacheOperationType OperationType { get; set; }
    public bool? IsHit { get; set; }
    public double DurationMs { get; set; }
    public string? ValueTypeName { get; set; }
    public long? EstimatedSizeBytes { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public enum CacheOperationType
{
    Get,
    Set,
    Remove
}