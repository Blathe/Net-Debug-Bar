namespace NetDebugBar.Utils;

public static class DebugBarUtils
{
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string FormatDuration(double ms)
    {
        if (ms < 1000)
            return $"{ms:0.##}ms";
        return $"{(ms / 1000):0.##}s";
    }
}
