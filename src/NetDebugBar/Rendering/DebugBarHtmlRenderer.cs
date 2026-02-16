using System.Text;
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

    public string Render(NetDebugBarContext debug)
    {
        var sb = new StringBuilder();

        // Inline CSS
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style>");

        // Container
        sb.AppendLine("<div id=\"ndb-bar\">");

        // Tab header
        sb.AppendLine("<div id=\"ndb-header\">");
        bool first = true;
        foreach (var panel in _panels)
        {
            var badge = panel.RenderBadge(debug);
            var badgeHtml = badge != null ? $" ({Encode(badge)})" : "";
            var activeClass = first ? " active" : "";
            var brandStyle = first ? $" style=\"background-color:{_options.AccentColor};font-weight:bold;\"" : "";
            sb.AppendLine($"<div class=\"ndb-tab{activeClass}\" data-tab=\"{panel.TabId}\"{brandStyle}>{Encode(panel.TabLabel)}{badgeHtml}</div>");
            first = false;
        }
        sb.AppendLine("<div id=\"ndb-toggle\">&#8722;</div>");
        sb.AppendLine("</div>");

        // Tab content
        sb.AppendLine("<div id=\"ndb-content\">");
        first = true;
        foreach (var panel in _panels)
        {
            var activeClass = first ? " active" : "";
            sb.AppendLine($"<div class=\"ndb-tab-content{activeClass}\" id=\"ndb-tab-{panel.TabId}\">");
            sb.AppendLine(panel.RenderTabContent(debug));
            sb.AppendLine("</div>");
            first = false;
        }
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        // Inline JS
        sb.AppendLine("<script>");
        sb.AppendLine(GetJs());
        sb.AppendLine("</script>");

        return sb.ToString();
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);

    private string GetCss()
    {
        return @"
#ndb-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    width: 100%;
    background: rgba(0,0,0,0.9);
    color: #fff;
    font-size: 12px;
    z-index: 9999;
    height: 200px;
    max-height: 60vh;
    display: flex;
    flex-direction: column;
    transition: height 0.3s ease;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

#ndb-bar.collapsed {
    height: 30px;
}

#ndb-header::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    height: 5px;
    width: 100%;
    background-color: purple;
}

#ndb-header {
    display: flex;
    background: #222;
    border-bottom: 1px solid #444;
    flex-shrink: 0;
    position: relative;
}

.ndb-tab {
    padding: 8px 12px;
    cursor: pointer;
    border-right: 1px solid #444;
    white-space: nowrap;
    user-select: none;
}

.ndb-tab:hover {
    background: #333;
}

.ndb-tab.active {
    background: #000;
    font-weight: bold;
    color: #0ff;
}

#ndb-content {
    flex: 1;
    overflow-y: auto;
    padding: 8px;
    min-height: 0;
}

#ndb-content::-webkit-scrollbar {
    width: 8px;
}

#ndb-content::-webkit-scrollbar-track {
    background: #111;
}

#ndb-content::-webkit-scrollbar-thumb {
    background: #555;
    border-radius: 4px;
}

#ndb-content::-webkit-scrollbar-thumb:hover {
    background: #777;
}

.ndb-tab-content {
    display: none;
}

.ndb-tab-content.active {
    display: block;
}

#ndb-toggle {
    position: absolute;
    right: 0;
    top: 0;
    padding: 8px 12px;
    cursor: pointer;
    background: #111;
    border-left: 1px solid #444;
    user-select: none;
}

#ndb-toggle:hover {
    background: #222;
}

/* Dashboard Styles */
.ndb-dashboard {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
    gap: 8px;
}

.ndb-card {
    background: rgba(255, 255, 255, 0.05);
    padding: 6px 8px;
    border-left: 3px solid #555;
    border-radius: 2px;
}

.ndb-card.accent {
    border-left-color: #a855f7;
}

.ndb-card-label {
    font-size: 9px;
    color: #999;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    margin-bottom: 3px;
}

.ndb-card-value {
    font-size: 14px;
    font-weight: bold;
    color: #0ff;
}

.ndb-card-subtext {
    font-size: 9px;
    color: #666;
    margin-top: 2px;
}

/* Query Styles */
.ndb-query-good {
    color: #6bff6b;
}

.ndb-query-medium {
    color: #ffe066;
}

.ndb-query-slow {
    color: #ff6b6b;
    font-weight: bold;
}

.ndb-query-item {
    margin: 3px 0;
    padding: 3px 0;
    border-bottom: 1px solid #333;
    word-break: break-word;
    font-family: 'Courier New', monospace;
    font-size: 11px;
    line-height: 1.4;
}

.ndb-query-item:last-child {
    border-bottom: none;
}

.ndb-query-duration {
    display: inline-block;
    min-width: 60px;
    font-weight: bold;
    color: #ccc;
}

.ndb-query-sql {
    display: inline;
    margin-left: 8px;
    color: #ddd;
}

.ndb-section-header {
    border-bottom: 1px solid #555;
    padding-bottom: 4px;
    margin: 8px 0 6px 0;
    font-weight: bold;
    color: #aaa;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.ndb-queries-list {
    margin: 0;
    padding-left: 15px;
}

.ndb-queries-list li {
    margin: 4px 0;
}

/* Request Panel Styles */
.ndb-info-section {
    margin: 6px 0;
    padding: 4px 0;
    border-bottom: 1px solid #333;
}

.ndb-info-label {
    font-size: 9px;
    color: #999;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    margin-bottom: 3px;
}

.ndb-info-value {
    font-size: 12px;
    color: #ddd;
    font-family: 'Courier New', monospace;
}

.ndb-expandable {
    margin: 6px 0;
    padding: 4px 0;
    border-bottom: 1px solid #333;
}

.ndb-expandable summary {
    cursor: pointer;
    font-size: 10px;
    color: #aaa;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    user-select: none;
    padding: 3px 0;
}

.ndb-expandable summary:hover {
    color: #0ff;
}

.ndb-table {
    width: 100%;
    margin-top: 4px;
    border-collapse: collapse;
    font-size: 11px;
}

.ndb-table tr {
    border-bottom: 1px solid #2a2a2a;
}

.ndb-table tr:last-child {
    border-bottom: none;
}

.ndb-table th {
    padding: 4px 8px 4px 0;
    text-align: left;
    color: #999;
    font-weight: normal;
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    border-bottom: 1px solid #444;
}

.ndb-table td {
    padding: 3px 8px 3px 0;
    color: #ddd;
    vertical-align: top;
}

.ndb-table-key {
    padding: 4px 8px 4px 0;
    color: #6af;
    font-family: 'Courier New', monospace;
    vertical-align: top;
    width: 30%;
}

.ndb-table-value {
    padding: 4px 0;
    color: #ddd;
    word-break: break-all;
}

.ndb-cache-table {
    font-family: 'Courier New', monospace;
}

.ndb-cache-table td {
    padding: 2px 6px 2px 0;
    font-size: 10px;
}

/* Timeline Styles */
.ndb-timeline {
    margin: 6px 0;
}

.ndb-timeline-row {
    display: grid;
    grid-template-columns: 200px 1fr 80px;
    gap: 8px;
    align-items: center;
    margin: 3px 0;
    padding: 3px 0;
}

.ndb-timeline-label {
    font-size: 11px;
    color: #ddd;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.ndb-timeline-track {
    position: relative;
    height: 16px;
    background: rgba(255,255,255,0.05);
    border-radius: 2px;
    overflow: visible;
}

.ndb-timeline-bar {
    position: absolute;
    height: 100%;
    border-radius: 2px;
    transition: opacity 0.2s;
}

.ndb-timeline-bar:hover {
    opacity: 0.8;
    cursor: pointer;
}

.ndb-timeline-time {
    font-size: 11px;
    color: #999;
    text-align: right;
    font-family: 'Courier New', monospace;
}
";
    }

    private string GetJs()
    {
        return @"
(function() {
    const tabs = document.querySelectorAll('#ndb-bar .ndb-tab');
    const contents = document.querySelectorAll('#ndb-bar .ndb-tab-content');
    const toggle = document.getElementById('ndb-toggle');
    const bar = document.getElementById('ndb-bar');

    // Tab switching
    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            const target = tab.dataset.tab;
            if (!target) return;

            // Activate clicked tab
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // Show corresponding content
            contents.forEach(c => c.classList.remove('active'));
            const targetContent = document.getElementById('ndb-tab-' + target);
            if (targetContent) {
                targetContent.classList.add('active');
            }
        });
    });

    // Toggle collapse/expand
    if (toggle && bar) {
        toggle.addEventListener('click', () => {
            bar.classList.toggle('collapsed');
            toggle.textContent = bar.classList.contains('collapsed') ? '+' : '−';
        });
    }

    // Log level filtering
    document.querySelectorAll('.ndb-log-filter').forEach(btn => {
        btn.addEventListener('click', function() {
            this.classList.toggle('active');
            const level = this.dataset.level;
            const display = this.classList.contains('active') ? '' : 'none';
            document.querySelectorAll('.ndb-log-entry[data-level=""' + level + '""]').forEach(entry => {
                entry.style.display = display;
            });
        });
    });
})();
";
    }
}
