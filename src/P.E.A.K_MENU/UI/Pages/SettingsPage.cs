using P.E.A.K_MENU.Constants;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class SettingsPage : IMenuPage
{
    private bool _waitingForKey;

    public string Title => "设置";

    public void Draw(MenuStyles styles)
    {
        GUILayout.Label(
            "菜单外观、尺寸与快捷键设置。",
            styles.MutedLabel
        );

        GUILayout.Space(14f);

        DrawThemeSection(styles);

        GUILayout.Space(18f);

        DrawShortcutSection(styles);

        GUILayout.Space(18f);

        DrawWindowSizeSection(styles);
    }

    private static void DrawThemeSection(
        MenuStyles styles)
    {
        GUILayout.Label(
            "主题色",
            styles.Label
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            $"当前主题：{GetThemeDisplayName(MenuSettings.Theme)}",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();

        DrawThemeButton(
            styles,
            MenuTheme.Iris,
            "鸢尾花"
        );

        DrawThemeButton(
            styles,
            MenuTheme.Ocean,
            "海洋"
        );

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        DrawThemeButton(
            styles,
            MenuTheme.Emerald,
            "翡翠"
        );

        DrawThemeButton(
            styles,
            MenuTheme.Rose,
            "玫瑰"
        );

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        DrawThemeButton(
            styles,
            MenuTheme.Amber,
            "琥珀"
        );

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        if (GUILayout.Button(
                "恢复默认主题（鸢尾花）",
                styles.ActionButton,
                GUILayout.Height(36f)))
        {
            MenuSettings.ResetTheme();
        }
    }

    private static void DrawThemeButton(
        MenuStyles styles,
        MenuTheme theme,
        string label)
    {
        bool selected =
            MenuSettings.Theme == theme;

        GUIStyle style = selected
            ? styles.ThemeButtonSelected
            : styles.ThemeButton;

        if (GUILayout.Button(
                label,
                style,
                GUILayout.Height(38f),
                GUILayout.ExpandWidth(true)))
        {
            MenuSettings.SetTheme(theme);
        }
    }

    private void DrawShortcutSection(
        MenuStyles styles)
    {
        GUILayout.Label(
            "菜单快捷键",
            styles.Label
        );

        GUILayout.Space(6f);

        string buttonText = _waitingForKey
            ? "请按下新的快捷键..."
            : $"当前快捷键：{MenuSettings.ToggleKey}";

        if (GUILayout.Button(
                buttonText,
                styles.ActionButton,
                GUILayout.Height(40f)))
        {
            BeginRebind();
        }

        if (_waitingForKey)
        {
            GUILayout.Label(
                "按下任意按键完成绑定，按 Esc 取消。",
                styles.MutedLabel
            );

            CaptureKeyEvent();
        }

        if (GUILayout.Button(
                $"恢复默认快捷键（{ModConstants.DefaultToggleMenuKey}）",
                styles.ActionButton,
                GUILayout.Height(36f)))
        {
            CancelRebind();
            MenuSettings.ResetToggleKey();
        }
    }

    private static void DrawWindowSizeSection(
        MenuStyles styles)
    {
        GUILayout.Label(
            "窗口尺寸",
            styles.Label
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            $"当前大小：{MenuSettings.WindowWidth:0} × " +
            $"{MenuSettings.WindowHeight:0}",
            styles.MutedLabel
        );

        GUILayout.Label(
            "拖动窗口右下角的小三角可以调整大小。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        if (GUILayout.Button(
                "恢复默认窗口大小",
                styles.ActionButton,
                GUILayout.Height(36f)))
        {
            MenuSettings.ResetWindowSize();
        }
    }

    private void BeginRebind()
    {
        _waitingForKey = true;
        MenuState.IsRebinding = true;

        Plugin.Log.LogInfo(
            "Waiting for a new menu shortcut key."
        );
    }

    private void CancelRebind()
    {
        _waitingForKey = false;
        MenuState.IsRebinding = false;
    }

    private void CaptureKeyEvent()
    {
        Event currentEvent = Event.current;

        if (currentEvent is null ||
            currentEvent.type != EventType.KeyDown)
        {
            return;
        }

        KeyCode pressedKey =
            currentEvent.keyCode;

        if (pressedKey == KeyCode.None)
        {
            return;
        }

        if (pressedKey == KeyCode.Escape)
        {
            CancelRebind();
            currentEvent.Use();
            return;
        }

        MenuSettings.SetToggleKey(
            pressedKey
        );

        CancelRebind();
        currentEvent.Use();
    }

    private static string GetThemeDisplayName(
        MenuTheme theme)
    {
        return theme switch
        {
            MenuTheme.Iris => "鸢尾花",
            MenuTheme.Ocean => "海洋",
            MenuTheme.Emerald => "翡翠",
            MenuTheme.Rose => "玫瑰",
            MenuTheme.Amber => "琥珀",
            _ => "鸢尾花"
        };
    }
}