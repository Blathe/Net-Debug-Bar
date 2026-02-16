using System.Reflection;
using NetDebugBar.Core;
using NetDebugBar.Rendering.Panels;

namespace NetDebugBar.Rendering;

/// <summary>
/// Renders the debug bar HTML, CSS, and JavaScript.
/// </summary>
public class DebugBarHtmlRenderer
{
    private readonly NetDebugBarOptions _options;
    private readonly List<IDebugBarPanel> _panels;
    private static readonly string Css = LoadEmbeddedResource("styles.css");
    private static readonly string Js = LoadEmbeddedResource("scripts.js");

    public DebugBarHtmlRenderer(NetDebugBarOptions options)
    {
        _options = options;
        _panels = new List<IDebugBarPanel>();

        // Always add overview
        _panels.Add(new OverviewPanel(options));

        // Add enabled panels
        if (options.EnableRequestPanel)
            _panels.Add(new RequestPanel());

        if (options.EnableQueryPanel)
            _panels.Add(new QueriesPanel(options));

        if (options.EnableCachePanel)
            _panels.Add(new CachePanel());

        if (options.EnableLoggingPanel)
            _panels.Add(new LoggingPanel());

        if (options.EnableTimelinePanel)
            _panels.Add(new TimelinePanel());
    }

    private static string LoadEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"NetDebugBar.Rendering.Templates.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public string Render(NetDebugBarContext debug)
    {
        var tabs = string.Join("", _panels.Select((panel, index) => RenderTab(panel, debug, index == 0)));
        var content = string.Join("", _panels.Select((panel, index) => RenderTabContent(panel, debug, index == 0)));

        return $$"""
            <style>{{Css}}</style>
            <div id="ndb-bar">
                <div id="ndb-header">
                    {{tabs}}
                    <div id="ndb-toggle">&#8722;</div>
                </div>
                <div id="ndb-content">
                    {{content}}
                </div>
            </div>
            <script>{{Js}}</script>
            """;
    }

    private string RenderTab(IDebugBarPanel panel, NetDebugBarContext debug, bool isFirst)
    {
        var badge = panel.RenderBadge(debug);
        var badgeHtml = badge != null ? $" ({Encode(badge)})" : "";
        var activeClass = isFirst ? " active" : "";
        var brandStyle = isFirst ? $" style=\"background-color:{_options.AccentColor};font-weight:bold;\"" : "";

        return $"<div class=\"ndb-tab{activeClass}\" data-tab=\"{panel.TabId}\"{brandStyle}>{Encode(panel.TabLabel)}{badgeHtml}</div>";
    }

    private string RenderTabContent(IDebugBarPanel panel, NetDebugBarContext debug, bool isFirst)
    {
        var activeClass = isFirst ? " active" : "";
        return $$"""
            <div class="ndb-tab-content{{activeClass}}" id="ndb-tab-{{panel.TabId}}">
                {{panel.RenderTabContent(debug)}}
            </div>
            """;
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}
