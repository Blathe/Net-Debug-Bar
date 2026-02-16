namespace NetDebugBar.Core.Models;

public class EntityModelInfo
{
    public string TypeName { get; set; } = "";
    public int Count { get; set; }
    public int AddedCount { get; set; }
    public int ModifiedCount { get; set; }
    public int DeletedCount { get; set; }
    public int UnchangedCount { get; set; }
}
