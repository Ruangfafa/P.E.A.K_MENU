using System;
using BepInEx.Configuration;
using P.E.A.K_MENU.Features.Flight;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU;

/// <summary>
/// 在 Mod 版本变化后恢复与布局相关的默认设置。
/// </summary>
internal static class ModUpdateSettings
{
    private static ConfigFile? _config;

    private static ConfigEntry<string>?
        _lastRunVersion;

    private static ConfigEntry<string>?
        _lastAcknowledgedChangelogVersion;

    private static ConfigEntry<string>?
        _lastAcknowledgedAnnouncementVersion;

    private static ConfigEntry<string>?
        _lastHoverDownForceResetVersion;

    private static ConfigEntry<bool>?
        _showChangelogOnEveryLaunch;

    private static string _currentVersion =
        string.Empty;

    internal static string CurrentVersion =>
        _currentVersion;

    internal static bool ShouldShowChangelog =>
        !string.IsNullOrWhiteSpace(
            _currentVersion
        ) &&
        ((_showChangelogOnEveryLaunch?.Value ??
          false) ||
         !string.Equals(
             _lastAcknowledgedChangelogVersion
                 ?.Value,
             _currentVersion,
             StringComparison.OrdinalIgnoreCase
         ));

    internal static void Apply(
        ConfigFile config,
        string currentVersion)
    {
        _config = config;

        _lastRunVersion = config.Bind(
            "Internal",
            "LastRunVersion",
            string.Empty,
            "上次成功应用版本更新默认设置时的 Mod 版本。"
        );

        _lastAcknowledgedChangelogVersion =
            config.Bind(
                "Internal",
                "LastAcknowledgedChangelogVersion",
                string.Empty,
                "上次已确认关闭更新日志的 Mod 版本。"
            );

        _lastAcknowledgedAnnouncementVersion =
            config.Bind(
                "Internal",
                "LastAcknowledgedAnnouncementVersion",
                string.Empty,
                "上次已确认关闭版本公告的 Mod 版本。"
            );

        _lastHoverDownForceResetVersion =
            config.Bind(
                "Internal",
                "LastHoverDownForceResetVersion",
                string.Empty,
                "上次自动恢复浮空重力默认值时的 Mod 版本。"
            );

        _showChangelogOnEveryLaunch =
            config.Bind(
                "Debug",
                "ShowChangelogOnEveryLaunch",
                false,
                "调试选项：开启后每次启动游戏都显示更新日志和当前版本公告。"
            );

        string normalizedVersion =
            currentVersion.Trim();

        _currentVersion =
            normalizedVersion;

        if (string.IsNullOrEmpty(
                normalizedVersion))
        {
            Plugin.Log.LogWarning(
                "Could not resolve the current Mod version; " +
                "update defaults were not applied."
            );

            return;
        }

        ResetHoverDownForceForVersion(
            config,
            normalizedVersion
        );

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

    internal static void AcknowledgeChangelog()
    {
        if (_lastAcknowledgedChangelogVersion
                is null ||
            string.IsNullOrWhiteSpace(
                _currentVersion
            ))
        {
            return;
        }

        _lastAcknowledgedChangelogVersion.Value =
            _currentVersion;

        _config?.Save();

        Plugin.Log.LogInfo(
            $"Changelog acknowledged for version " +
            $"{_currentVersion}."
        );
    }

    private static void ResetHoverDownForceForVersion(
        ConfigFile config,
        string normalizedVersion)
    {
        if (_lastHoverDownForceResetVersion
                is null ||
            string.Equals(
                _lastHoverDownForceResetVersion.Value,
                normalizedVersion,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return;
        }

        if (!FlightRuntime.IsInitialized)
        {
            Plugin.Log.LogWarning(
                "Flight configuration is unavailable; " +
                "the hover calibration reset will be " +
                "retried next launch."
            );

            return;
        }

        FlightRuntime.Service.ResetHoverDownForce();

        _lastHoverDownForceResetVersion.Value =
            normalizedVersion;

        config.Save();

        Plugin.Log.LogInfo(
            $"Hover down force was reset to " +
            $"{FlightService.DefaultHoverDownForce:0.##} " +
            $"for version {normalizedVersion}."
        );
    }

    internal static bool ShouldShowAnnouncement(
        string announcementVersion)
    {
        return
            !string.IsNullOrWhiteSpace(
                announcementVersion
            ) &&
            string.Equals(
                announcementVersion,
                _currentVersion,
                StringComparison.OrdinalIgnoreCase
            ) &&
            ((_showChangelogOnEveryLaunch?.Value ??
              false) ||
             !string.Equals(
                 _lastAcknowledgedAnnouncementVersion
                     ?.Value,
                 announcementVersion,
                 StringComparison.OrdinalIgnoreCase
             ));
    }

    internal static void AcknowledgeAnnouncement(
        string announcementVersion)
    {
        if (_lastAcknowledgedAnnouncementVersion
                is null ||
            !string.Equals(
                announcementVersion,
                _currentVersion,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return;
        }

        _lastAcknowledgedAnnouncementVersion.Value =
            announcementVersion;

        _config?.Save();

        Plugin.Log.LogInfo(
            $"Announcement acknowledged for version " +
            $"{announcementVersion}."
        );
    }
}
