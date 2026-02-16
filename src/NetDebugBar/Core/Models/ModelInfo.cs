namespace NetDebugBar.Core.Models;

public class ModelInfo
{
    public string? PageModelType { get; set; }
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public List<EntityModelInfo> EntityModels { get; set; } = new();
}
