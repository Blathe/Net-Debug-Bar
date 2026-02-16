using NetDebugBar.Core;

namespace NetDebugBar.Rendering.Panels;

public interface IDebugBarPanel
{
    string TabId { get; }
    string TabLabel { get; }
    string RenderTabContent(NetDebugBarContext debug);
    string? RenderBadge(NetDebugBarContext debug);
}
