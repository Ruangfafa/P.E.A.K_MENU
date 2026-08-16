using System;
using System.IO;
using System.Reflection;
using P.E.A.K_MENU.Constants;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

/// <summary>
/// 在游戏界面上方显示内嵌更新日志的居中模态框。
/// </summary>
internal sealed class ChangelogOverlay :
    IDisposable
{
    private const string ChangelogResourceName =
        "P.E.A.K_MENU.CHANGELOG.md";

    private const float MaximumWidth = 760f;
    private const float MaximumHeight = 720f;
    private const float ScreenMargin = 32f;

    private readonly CursorController
        _cursorController = new();

    private readonly string[] _lines;

    private MenuStyles? _styles;
    private MenuTheme _loadedTheme;
    private GUIStyle? _versionHeadingStyle;
    private GUIStyle? _sectionHeadingStyle;

    private Vector2 _scrollPosition;
    private bool _isVisible;
    private bool _disposed;

    internal bool IsVisible =>
        _isVisible && !_disposed;

    private MenuStyles Styles
    {
        get
        {
            EnsureCurrentTheme();
            return _styles!;
        }
    }

    internal ChangelogOverlay()
    {
        _loadedTheme =
            MenuSettings.Theme;

        _lines = LoadChangelogLines();

        if (ModUpdateSettings
                .ShouldShowChangelog)
        {
            Show();
        }
    }

    internal void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (UnityEngine.Input.GetKeyDown(
                KeyCode.Escape))
        {
            CloseAndAcknowledge();
        }
    }

    internal void LateUpdate()
    {
        if (IsVisible)
        {
            _cursorController
                .MaintainReleased();
        }
    }

    internal void Draw()
    {
        if (!IsVisible)
        {
            return;
        }

        EnsureCurrentTheme();

        Color previousColor =
            GUI.color;

        GUI.color =
            new Color(0f, 0f, 0f, 0.68f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height
            ),
            Texture2D.whiteTexture,
            ScaleMode.StretchToFill
        );

        GUI.color = previousColor;

        float width = Mathf.Min(
            MaximumWidth,
            Mathf.Max(
                320f,
                Screen.width -
                ScreenMargin * 2f
            )
        );

        float height = Mathf.Min(
            MaximumHeight,
            Mathf.Max(
                300f,
                Screen.height -
                ScreenMargin * 2f
            )
        );

        width = Mathf.Min(
            width,
            Screen.width
        );

        height = Mathf.Min(
            height,
            Screen.height
        );

        Rect windowRect = new(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.ModalWindow(
            ModConstants.ChangelogWindowId,
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

    private void Show()
    {
        if (_disposed || _isVisible)
        {
            return;
        }

        _isVisible = true;
        _scrollPosition = Vector2.zero;

        MenuState.IsOpen = true;

        _cursorController.Release();

        Plugin.Log.LogInfo(
            $"Showing changelog for version " +
            $"{ModUpdateSettings.CurrentVersion}."
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

        if (acknowledge)
        {
            ModUpdateSettings
                .AcknowledgeChangelog();
        }

        _isVisible = false;

        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _cursorController.Restore();
    }

    private void DrawWindowContents(
        int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(
            "P.E.A.K MENU 更新日志",
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

        GUILayout.Space(4f);

        GUILayout.Label(
            $"当前版本：{ModUpdateSettings.CurrentVersion}",
            Styles.MutedLabel
        );

        GUILayout.Space(12f);

        _scrollPosition =
            GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        DrawMarkdownContents();

        GUILayout.EndScrollView();

        GUILayout.Space(12f);

        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "按 Esc 也可以关闭",
            Styles.MutedLabel,
            GUILayout.Height(40f),
            GUILayout.ExpandWidth(true)
        );

        if (GUILayout.Button(
                "确认",
                Styles.ActionButton,
                GUILayout.Width(150f),
                GUILayout.Height(40f)))
        {
            CloseAndAcknowledge();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawMarkdownContents()
    {
        foreach (string rawLine in _lines)
        {
            string line =
                rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(
                    line))
            {
                GUILayout.Space(6f);
                continue;
            }

            if (line.StartsWith(
                    "# ",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith(
                    "## ",
                    StringComparison.Ordinal))
            {
                GUILayout.Space(8f);

                GUILayout.Label(
                    RemoveInlineMarkdown(
                        line.Substring(3)
                    ),
                    _versionHeadingStyle!
                );

                continue;
            }

            if (line.StartsWith(
                    "### ",
                    StringComparison.Ordinal))
            {
                GUILayout.Space(4f);

                GUILayout.Label(
                    RemoveInlineMarkdown(
                        line.Substring(4)
                    ),
                    _sectionHeadingStyle!
                );

                continue;
            }

            string displayText =
                line.StartsWith(
                    "- ",
                    StringComparison.Ordinal)
                    ? "• " + line.Substring(2)
                    : line;

            GUILayout.Label(
                RemoveInlineMarkdown(
                    displayText
                ),
                Styles.Label
            );
        }
    }

    private void EnsureCurrentTheme()
    {
        MenuTheme currentTheme =
            MenuSettings.Theme;

        if (_styles is not null &&
            _loadedTheme == currentTheme)
        {
            return;
        }

        _styles?.Dispose();

        _loadedTheme = currentTheme;
        _styles = new MenuStyles(
            currentTheme
        );

        _versionHeadingStyle =
            new GUIStyle(_styles.Title)
            {
                fontSize = 18,
                alignment =
                    TextAnchor.MiddleLeft
            };

        _sectionHeadingStyle =
            new GUIStyle(_styles.Label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15
            };
    }

    private static string RemoveInlineMarkdown(
        string value)
    {
        return value.Replace(
            "`",
            string.Empty
        );
    }

    private static string[] LoadChangelogLines()
    {
        try
        {
            Assembly assembly =
                typeof(ChangelogOverlay)
                    .Assembly;

            using Stream? stream =
                assembly.GetManifestResourceStream(
                    ChangelogResourceName
                );

            if (stream is null)
            {
                throw new InvalidOperationException(
                    $"Embedded resource " +
                    $"'{ChangelogResourceName}' was not found."
                );
            }

            using var reader =
                new StreamReader(stream);

            return reader
                .ReadToEnd()
                .Replace("\r\n", "\n")
                .Split('\n');
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to load embedded changelog: " +
                $"{exception}"
            );

            return new[]
            {
                "更新日志读取失败。",
                "请查看 Mod 包内的 CHANGELOG.md。"
            };
        }
    }
}
