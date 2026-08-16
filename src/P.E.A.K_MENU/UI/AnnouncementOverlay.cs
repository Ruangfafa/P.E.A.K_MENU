using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using P.E.A.K_MENU.Constants;
using P.E.A.K_MENU.Features.ItemSpawn;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

/// <summary>
/// 在更新日志之后显示当前版本命中的版本公告。
/// 公告内容和快捷操作由内嵌的 ANNOUNCEMENTS.md 定义。
/// </summary>
internal sealed class AnnouncementOverlay :
    IDisposable
{
    private const string AnnouncementResourceName =
        "P.E.A.K_MENU.ANNOUNCEMENTS.md";

    private const float MaximumWidth = 700f;
    private const float MaximumHeight = 560f;
    private const float ScreenMargin = 32f;

    private readonly CursorController
        _cursorController = new();

    private readonly AnnouncementDefinition?
        _announcement;

    private MenuStyles? _styles;
    private MenuTheme _loadedTheme;
    private GUIStyle? _headingStyle;

    private Vector2 _scrollPosition;
    private float _shownAt;
    private string _actionStatus = string.Empty;
    private bool _showAttempted;
    private bool _isVisible;
    private bool _disposed;

    internal bool IsVisible =>
        _isVisible && !_disposed;

    private bool CanClose =>
        _announcement is not null &&
        Time.realtimeSinceStartup - _shownAt >=
        _announcement.WaitSeconds;

    private int RemainingWaitSeconds =>
        _announcement is null
            ? 0
            : Mathf.Max(
                0,
                Mathf.CeilToInt(
                    _announcement.WaitSeconds -
                    (Time.realtimeSinceStartup - _shownAt)
                )
            );

    private MenuStyles Styles
    {
        get
        {
            EnsureCurrentTheme();
            return _styles!;
        }
    }

    internal AnnouncementOverlay()
    {
        _loadedTheme = MenuSettings.Theme;
        _announcement = LoadCurrentAnnouncement();
    }

    internal void Update(
        bool canShow)
    {
        if (!IsVisible)
        {
            if (canShow && !_showAttempted)
            {
                TryShow();
            }

            return;
        }

        if (CanClose &&
            UnityEngine.Input.GetKeyDown(
                KeyCode.Escape))
        {
            CloseAndAcknowledge();
        }
    }

    internal void LateUpdate()
    {
        if (IsVisible)
        {
            _cursorController.MaintainReleased();
        }
    }

    internal void Draw()
    {
        if (!IsVisible || _announcement is null)
        {
            return;
        }

        EnsureCurrentTheme();

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.68f);

        GUI.DrawTexture(
            new Rect(0f, 0f, Screen.width, Screen.height),
            Texture2D.whiteTexture,
            ScaleMode.StretchToFill
        );

        GUI.color = previousColor;

        float width = Mathf.Min(
            MaximumWidth,
            Mathf.Max(
                320f,
                Screen.width - ScreenMargin * 2f
            )
        );

        float height = Mathf.Min(
            MaximumHeight,
            Mathf.Max(
                300f,
                Screen.height - ScreenMargin * 2f
            )
        );

        width = Mathf.Min(width, Screen.width);
        height = Mathf.Min(height, Screen.height);

        var windowRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.ModalWindow(
            ModConstants.AnnouncementWindowId,
            windowRect,
            DrawWindowContents,
            GUIContent.none,
            Styles.Window
        );
    }

    internal void HideWithoutAcknowledging()
    {
        Hide(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Hide(false);
        _styles?.Dispose();
        _styles = null;
        _disposed = true;
    }

    private void TryShow()
    {
        _showAttempted = true;

        if (_disposed ||
            _announcement is null ||
            !ModUpdateSettings.ShouldShowAnnouncement(
                _announcement.Version))
        {
            return;
        }

        _isVisible = true;
        _scrollPosition = Vector2.zero;
        _actionStatus = string.Empty;
        _shownAt = Time.realtimeSinceStartup;

        MenuState.IsOpen = true;
        _cursorController.Release();

        Plugin.Log.LogInfo(
            $"Showing announcement for version " +
            $"{_announcement.Version}."
        );
    }

    private void CloseAndAcknowledge()
    {
        Hide(true);
    }

    private void Hide(
        bool acknowledge)
    {
        if (!_isVisible)
        {
            return;
        }

        if (acknowledge && _announcement is not null)
        {
            ModUpdateSettings.AcknowledgeAnnouncement(
                _announcement.Version
            );
        }

        _isVisible = false;
        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;
        _cursorController.Restore();
    }

    private void DrawWindowContents(
        int windowId)
    {
        if (_announcement is null)
        {
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(
            "P.E.A.K MENU 公告",
            Styles.WindowTitleBold,
            GUILayout.ExpandWidth(false)
        );

        GUILayout.Space(14f);

        GUILayout.Label(
            ModConstants.WindowAuthorText,
            Styles.WindowTitleNormal,
            GUILayout.ExpandWidth(false)
        );

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(14f);

        GUILayout.Label(
            _announcement.Title,
            _headingStyle!
        );

        GUILayout.Space(10f);

        _scrollPosition = GUILayout.BeginScrollView(
            _scrollPosition,
            false,
            true,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );

        DrawBody(_announcement.BodyLines);

        GUILayout.EndScrollView();

        foreach (AnnouncementAction action
                 in _announcement.Actions)
        {
            if (GUILayout.Button(
                    action.Label,
                    Styles.ActionButton,
                    GUILayout.Height(40f)))
            {
                ExecuteAction(action.Id);
            }
        }

        if (!string.IsNullOrWhiteSpace(
                _actionStatus))
        {
            GUILayout.Space(4f);
            GUILayout.Label(
                _actionStatus,
                Styles.MutedLabel
            );
        }

        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();

        string closeHint = CanClose
            ? "按 Esc 也可以关闭"
            : $"请阅读公告，{RemainingWaitSeconds} 秒后可关闭";

        GUILayout.Label(
            closeHint,
            Styles.MutedLabel,
            GUILayout.Height(40f),
            GUILayout.ExpandWidth(true)
        );

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && CanClose;

        string confirmText = CanClose
            ? "确认"
            : $"确认（{RemainingWaitSeconds}）";

        if (GUILayout.Button(
                confirmText,
                Styles.ActionButton,
                GUILayout.Width(150f),
                GUILayout.Height(40f)))
        {
            CloseAndAcknowledge();
        }

        GUI.enabled = previousEnabled;
        GUILayout.EndHorizontal();
    }

    private void ExecuteAction(
        string actionId)
    {
        switch (actionId)
        {
            case "restore-item-spawner-defaults":
                RestoreItemSpawnerDefaults();
                break;

            default:
                _actionStatus =
                    $"无法识别公告操作：{actionId}";

                Plugin.Log.LogWarning(
                    $"Unknown announcement action: " +
                    $"{actionId}"
                );
                break;
        }
    }

    private void RestoreItemSpawnerDefaults()
    {
        if (!ItemSpawnRuntime.IsInitialized)
        {
            _actionStatus =
                "物品生成器尚未初始化，暂时无法恢复默认列表。";
            return;
        }

        ItemSpawnRuntime.Catalog.RefreshIfNeeded();

        if (ItemSpawnRuntime.Catalog.AllItems.Count == 0)
        {
            _actionStatus =
                "游戏物品数据库尚未就绪，请进入游戏后再试。";
            return;
        }

        ItemSpawnRuntime.Catalog.RestoreDefaults();
        _actionStatus = "已恢复默认物品和默认排序。";
    }

    private void DrawBody(
        IEnumerable<string> lines)
    {
        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                GUILayout.Space(6f);
                continue;
            }

            string displayText = line.StartsWith(
                "- ",
                StringComparison.Ordinal)
                    ? "• " + line.Substring(2)
                    : line;

            GUILayout.Label(
                displayText.Replace("`", string.Empty),
                Styles.Label
            );
        }
    }

    private void EnsureCurrentTheme()
    {
        MenuTheme currentTheme = MenuSettings.Theme;

        if (_styles is not null &&
            _loadedTheme == currentTheme)
        {
            return;
        }

        _styles?.Dispose();
        _loadedTheme = currentTheme;
        _styles = new MenuStyles(currentTheme);
        _headingStyle = new GUIStyle(_styles.Title)
        {
            fontSize = 21,
            alignment = TextAnchor.MiddleLeft
        };
    }

    private static AnnouncementDefinition?
        LoadCurrentAnnouncement()
    {
        try
        {
            Assembly assembly =
                typeof(AnnouncementOverlay).Assembly;

            using Stream? stream =
                assembly.GetManifestResourceStream(
                    AnnouncementResourceName
                );

            if (stream is null)
            {
                throw new InvalidOperationException(
                    $"Embedded resource " +
                    $"'{AnnouncementResourceName}' was not found."
                );
            }

            using var reader = new StreamReader(stream);

            string[] lines = reader
                .ReadToEnd()
                .Replace("\r\n", "\n")
                .Split('\n');

            return ParseAnnouncements(lines)
                .FirstOrDefault(
                    announcement =>
                        string.Equals(
                            announcement.Version,
                            ModUpdateSettings.CurrentVersion,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to load embedded announcements: " +
                $"{exception}"
            );

            return null;
        }
    }

    private static IReadOnlyList<AnnouncementDefinition>
        ParseAnnouncements(
            IReadOnlyList<string> lines)
    {
        var announcements =
            new List<AnnouncementDefinition>();

        int index = 0;

        while (index < lines.Count)
        {
            string line = lines[index].Trim();

            if (!line.StartsWith(
                    "## ",
                    StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            string version = line.Substring(3).Trim();
            index++;

            string title = version;
            float waitSeconds = 0f;
            var actions = new List<AnnouncementAction>();

            while (index < lines.Count &&
                   !string.IsNullOrWhiteSpace(
                       lines[index]))
            {
                string metadata = lines[index].Trim();

                if (metadata.StartsWith(
                        "wait-seconds:",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string value = metadata.Substring(
                        "wait-seconds:".Length
                    ).Trim();

                    if (float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float parsedWait))
                    {
                        waitSeconds = Mathf.Max(0f, parsedWait);
                    }
                }
                else if (metadata.StartsWith(
                             "title:",
                             StringComparison.OrdinalIgnoreCase))
                {
                    title = metadata.Substring(
                        "title:".Length
                    ).Trim();
                }
                else if (metadata.StartsWith(
                             "action:",
                             StringComparison.OrdinalIgnoreCase))
                {
                    string value = metadata.Substring(
                        "action:".Length
                    ).Trim();

                    string[] parts = value.Split(
                        new[] { '|' },
                        2
                    );

                    if (parts.Length == 2 &&
                        !string.IsNullOrWhiteSpace(parts[0]) &&
                        !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        actions.Add(
                            new AnnouncementAction(
                                parts[0].Trim(),
                                parts[1].Trim()
                            )
                        );
                    }
                }

                index++;
            }

            if (index < lines.Count &&
                string.IsNullOrWhiteSpace(lines[index]))
            {
                index++;
            }

            var bodyLines = new List<string>();

            while (index < lines.Count &&
                   !lines[index].TrimStart().StartsWith(
                       "## ",
                       StringComparison.Ordinal))
            {
                bodyLines.Add(lines[index]);
                index++;
            }

            while (bodyLines.Count > 0 &&
                   string.IsNullOrWhiteSpace(
                       bodyLines[bodyLines.Count - 1]))
            {
                bodyLines.RemoveAt(bodyLines.Count - 1);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                announcements.Add(
                    new AnnouncementDefinition(
                        version,
                        title,
                        waitSeconds,
                        bodyLines,
                        actions
                    )
                );
            }
        }

        return announcements;
    }

    private sealed class AnnouncementDefinition
    {
        internal string Version { get; }
        internal string Title { get; }
        internal float WaitSeconds { get; }
        internal IReadOnlyList<string> BodyLines { get; }
        internal IReadOnlyList<AnnouncementAction> Actions { get; }

        internal AnnouncementDefinition(
            string version,
            string title,
            float waitSeconds,
            IReadOnlyList<string> bodyLines,
            IReadOnlyList<AnnouncementAction> actions)
        {
            Version = version;
            Title = title;
            WaitSeconds = waitSeconds;
            BodyLines = bodyLines;
            Actions = actions;
        }
    }

    private sealed class AnnouncementAction
    {
        internal string Id { get; }
        internal string Label { get; }

        internal AnnouncementAction(
            string id,
            string label)
        {
            Id = id;
            Label = label;
        }
    }
}
