using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class SettingsPage : IMenuPage
{
    private bool _waitingForKey;
    private bool _themeDropdownOpen;

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

    private void DrawThemeSection(MenuStyles styles)
    {
        GUILayout.Label("主题色", styles.Label);
        GUILayout.Space(6f);

        using (new GUILayout.HorizontalScope(GUILayout.Height(38f)))
        {
            if (GUILayout.Button(
                    GetThemeDisplayName(MenuSettings.Theme) + (_themeDropdownOpen ? " ▴" : " ▾"),
                    styles.ActionButton, GUILayout.Height(38f), GUILayout.ExpandWidth(true)))
            {
                _themeDropdownOpen = !_themeDropdownOpen;
            }
            if (GUILayout.Button("恢复默认", styles.ActionButton,
                    GUILayout.Width(100f), GUILayout.Height(38f)))
            {
                MenuSettings.ResetTheme();
                _themeDropdownOpen = false;
            }
        }

        if (!_themeDropdownOpen) return;
        foreach (MenuTheme theme in System.Enum.GetValues(typeof(MenuTheme)))
        {
            if (GUILayout.Button(GetThemeDisplayName(theme),
                    MenuSettings.Theme == theme ? styles.ThemeButtonSelected : styles.ThemeButton,
                    GUILayout.Height(34f), GUILayout.ExpandWidth(true)))
            {
                MenuSettings.SetTheme(theme);
                _themeDropdownOpen = false;
            }
        }
    }

    private void DrawShortcutSection(MenuStyles styles)
    {
        if (_waitingForKey) CaptureKeyEvent();
        GUILayout.Label("菜单快捷键", styles.Label);
        GUILayout.Space(6f);

        string buttonText = _waitingForKey
            ? "请按下新的快捷键..."
            : $"当前快捷键：{MenuSettings.ToggleKey}";

        using (new GUILayout.HorizontalScope(GUILayout.Height(38f)))
        {
            if (GUILayout.Button(buttonText, styles.ActionButton,
                    GUILayout.Height(38f), GUILayout.ExpandWidth(true)))
            {
                BeginRebind();
            }
            if (GUILayout.Button("恢复默认", styles.ActionButton,
                    GUILayout.Width(100f), GUILayout.Height(38f)))
            {
                CancelRebind();
                MenuSettings.ResetToggleKey();
            }
        }

        if (_waitingForKey)
        {
            GUILayout.Label("按下任意按键完成绑定，按 Esc 取消。", styles.MutedLabel);
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