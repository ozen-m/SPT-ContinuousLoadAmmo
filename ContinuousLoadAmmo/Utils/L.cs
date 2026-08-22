using System.Diagnostics;
using BepInEx.Logging;

namespace ContinuousLoadAmmo.Utils;

public static class L
{
    private static ManualLogSource _logger;

    public static void SetLogger(ManualLogSource logSource)
    {
        _logger = logSource;
    }

    [Conditional("DEBUG")]
    public static void Debug(string msg)
    {
        _logger.LogDebug(msg);
    }

    public static void Error(string msg)
    {
        _logger.LogError(msg);
    }

    public static void Warning(string msg)
    {
        _logger.LogWarning(msg);
    }

    public static void Info(string msg)
    {
        _logger.LogInfo(msg);
    }
}
