using BepInEx.Configuration;
using P.E.A.K_MENU.Constants;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal static class MenuSettings
{
    private static ConfigEntry<KeyCode>? _toggleKey;
    private static ConfigEntry<float>? _windowWidth;
    private static ConfigEntry<float>? _windowHeight;
    private static ConfigEntry<MenuTheme>? _theme;

    internal static KeyCode ToggleKey =>
        _toggleKey?.Value ??
        ModConstants.DefaultToggleMenuKey;

    internal static float WindowWidth =>
        Mathf.Clamp(
            _windowWidth?.Value ??
            ModConstants.DefaultWindowWidth,
            ModConstants.MinimumWindowWidth,
            ModConstants.MaximumWindowWidth
        );

    internal static float WindowHeight =>
        Mathf.Clamp(
            _windowHeight?.Value ??
            ModConstants.DefaultWindowHeight,
            ModConstants.MinimumWindowHeight,
            ModConstants.MaximumWindowHeight
        );

    internal static MenuTheme Theme =>
        _theme?.Value ??
        MenuTheme.Iris;

    internal static void Initialize(ConfigFile config)
    {
        _toggleKey = config.Bind(
            "Menu",
            "ToggleKey",
            ModConstants.DefaultToggleMenuKey,
            "打开或关闭 P.E.A.K_MENU 的快捷键。"
        );

        _windowWidth = config.Bind(
            "Menu",
            "WindowWidth",
            ModConstants.DefaultWindowWidth,
            "菜单窗口宽度。"
        );

        _windowHeight = config.Bind(
            "Menu",
            "WindowHeight",
            ModConstants.DefaultWindowHeight,
            "菜单窗口高度。"
        );

        _theme = config.Bind(
            "Appearance",
            "Theme",
            MenuTheme.Iris,
            "菜单主题颜色。"
        );

        ClampConfiguredSize();
    }

    internal static void SetToggleKey(KeyCode keyCode)
    {
        if (_toggleKey is null)
        {
            return;
        }

        _toggleKey.Value = keyCode;

        Plugin.Log.LogInfo(
            $"Menu toggle key changed to: {keyCode}"
        );
    }

    internal static void SetWindowSize(
        float width,
        float height)
    {
        float clampedWidth = Mathf.Clamp(
            width,
            ModConstants.MinimumWindowWidth,
            ModConstants.MaximumWindowWidth
        );

        float clampedHeight = Mathf.Clamp(
            height,
            ModConstants.MinimumWindowHeight,
            ModConstants.MaximumWindowHeight
        );

        if (_windowWidth is not null)
        {
            _windowWidth.Value = clampedWidth;
        }

        if (_windowHeight is not null)
        {
            _windowHeight.Value = clampedHeight;
        }
    }

    internal static void SetTheme(MenuTheme theme)
    {
        if (_theme is null)
        {
            return;
        }

        if (_theme.Value == theme)
        {
            return;
        }

        _theme.Value = theme;

        Plugin.Log.LogInfo(
            $"Menu theme changed to: {theme}"
        );
    }

    internal static void ResetWindowSize()
    {
        SetWindowSize(
            ModConstants.DefaultWindowWidth,
            ModConstants.DefaultWindowHeight
        );
    }

    internal static void ResetToggleKey()
    {
        SetToggleKey(
            ModConstants.DefaultToggleMenuKey
        );
    }

    internal static void ResetTheme()
    {
        SetTheme(MenuTheme.Iris);
    }

    private static void ClampConfiguredSize()
    {
        SetWindowSize(
            WindowWidth,
            WindowHeight
        );
    }
}