using System;
using BepInEx.Configuration;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU;

/// <summary>
/// 在 Mod 版本变化后恢复与布局相关的默认设置。
/// </summary>
internal static class ModUpdateSettings
{
    private static ConfigEntry<string>?
        _lastRunVersion;

    internal static void Apply(
        ConfigFile config,
        string currentVersion)
    {
        _lastRunVersion = config.Bind(
            "Internal",
            "LastRunVersion",
            string.Empty,
            "上次成功应用版本更新默认设置时的 Mod 版本。"
        );

        string normalizedVersion =
            currentVersion.Trim();

        if (string.IsNullOrEmpty(
                normalizedVersion))
        {
            Plugin.Log.LogWarning(
                "Could not resolve the current Mod version; " +
                "update defaults were not applied."
            );

            return;
        }

        if (string.Equals(
                _lastRunVersion.Value,
                normalizedVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!ItemSpawnConfiguration.IsInitialized)
        {
            Plugin.Log.LogWarning(
                "ItemSpawner configuration is unavailable; " +
                "update defaults will be retried next launch."
            );

            return;
        }

        string previousVersion =
            string.IsNullOrWhiteSpace(
                _lastRunVersion.Value)
                ? "<none>"
                : _lastRunVersion.Value;

        MenuSettings.ResetWindowSize();
        ItemSpawnConfiguration.ResetSpawnColumns();

        _lastRunVersion.Value =
            normalizedVersion;

        config.Save();

        Plugin.Log.LogInfo(
            $"Mod version changed from {previousVersion} " +
            $"to {normalizedVersion}; window size and " +
            $"ItemSpawner columns were reset to defaults."
        );
    }
}
