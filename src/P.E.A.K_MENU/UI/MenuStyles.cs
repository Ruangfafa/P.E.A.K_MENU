using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class MenuStyles
{
    internal GUIStyle Window { get; }
    internal GUIStyle Sidebar { get; }
    internal GUIStyle Content { get; }

    internal GUIStyle CategoryButton { get; }
    internal GUIStyle CategoryButtonSelected { get; }

    internal GUIStyle ActionButton { get; }
    internal GUIStyle Toggle { get; }

    internal GUIStyle ThemeButton { get; }
    internal GUIStyle ThemeButtonSelected { get; }

    internal GUIStyle Title { get; }
    internal GUIStyle SidebarTitle { get; }
    internal GUIStyle Label { get; }
    internal GUIStyle MutedLabel { get; }

    internal GUIStyle ResizeHandle { get; }

    internal GUIStyle WindowTitleBold { get; }
    internal GUIStyle WindowTitleNormal { get; }

    internal Texture2D SeparatorTexture { get; }

    private readonly Texture2D _windowBackground;
    private readonly Texture2D _sidebarBackground;
    private readonly Texture2D _contentBackground;

    private readonly Texture2D _buttonBackground;
    private readonly Texture2D _buttonHoverBackground;
    private readonly Texture2D _buttonActiveBackground;

    private readonly Texture2D _accentBackground;
    private readonly Texture2D _accentHoverBackground;

    private readonly Texture2D _toggleOffBackground;
    private readonly Texture2D _toggleHoverBackground;

    private readonly Texture2D _resizeBackground;
    private readonly Texture2D _resizeHoverBackground;

    internal MenuStyles(MenuTheme theme)
    {
        ThemePalette palette =
            ThemePalette.Create(theme);

        _windowBackground =
            CreateTexture(palette.Window);

        _sidebarBackground =
            CreateTexture(palette.Sidebar);

        _contentBackground =
            CreateTexture(palette.Content);

        _buttonBackground =
            CreateTexture(palette.Button);

        _buttonHoverBackground =
            CreateTexture(palette.ButtonHover);

        _buttonActiveBackground =
            CreateTexture(palette.ButtonActive);

        _accentBackground =
            CreateTexture(palette.Accent);

        _accentHoverBackground =
            CreateTexture(palette.AccentHover);

        _toggleOffBackground =
            CreateTexture(palette.ToggleOff);

        _toggleHoverBackground =
            CreateTexture(palette.ToggleHover);

        _resizeBackground =
            CreateTexture(palette.Resize);

        _resizeHoverBackground =
            CreateTexture(palette.AccentHover);

        SeparatorTexture =
            CreateTexture(palette.Separator);

        Window = CreateWindowStyle();
        Sidebar = CreateSidebarStyle();
        Content = CreateContentStyle();

        CategoryButton =
            CreateCategoryButtonStyle();

        CategoryButtonSelected =
            CreateSelectedCategoryButtonStyle();

        ActionButton =
            CreateActionButtonStyle();

        Toggle =
            CreateToggleStyle();

        ThemeButton =
            CreateThemeButtonStyle();

        ThemeButtonSelected =
            CreateSelectedThemeButtonStyle();

        Title =
            CreateTitleStyle();

        SidebarTitle =
            CreateSidebarTitleStyle();

        Label =
            CreateLabelStyle();

        MutedLabel =
            CreateMutedLabelStyle();

        ResizeHandle =
            CreateResizeHandleStyle();

        WindowTitleBold =
            CreateWindowTitleBoldStyle();

        WindowTitleNormal =
            CreateWindowTitleNormalStyle();
    }

    private GUIStyle CreateWindowStyle()
    {
        var style = new GUIStyle(GUI.skin.window)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperCenter,

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            padding = new RectOffset(
                12,
                12,
                30,
                12
            ),

            margin = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        ApplyBackgroundToAllStates(
            style,
            _windowBackground,
            Color.white
        );

        return style;
    }

    private GUIStyle CreateSidebarStyle()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            padding = new RectOffset(
                10,
                10,
                10,
                10
            ),

            margin = new RectOffset(
                0,
                8,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        ApplyBackgroundToAllStates(
            style,
            _sidebarBackground,
            Color.white
        );

        return style;
    }

    private GUIStyle CreateContentStyle()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            padding = new RectOffset(
                16,
                16,
                14,
                14
            ),

            margin = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        ApplyBackgroundToAllStates(
            style,
            _contentBackground,
            Color.white
        );

        return style;
    }

    private GUIStyle CreateCategoryButtonStyle()
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,

            padding = new RectOffset(
                14,
                10,
                0,
                0
            ),

            margin = new RectOffset(
                0,
                0,
                3,
                3
            ),

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14,
            fontStyle = FontStyle.Normal
        };

        Color normalText =
            new(0.84f, 0.86f, 0.90f);

        SetState(
            style.normal,
            _buttonBackground,
            normalText
        );

        SetState(
            style.hover,
            _buttonHoverBackground,
            Color.white
        );

        SetState(
            style.active,
            _buttonActiveBackground,
            Color.white
        );

        SetState(
            style.focused,
            _buttonBackground,
            normalText
        );

        SetState(
            style.onNormal,
            _buttonBackground,
            normalText
        );

        SetState(
            style.onHover,
            _buttonHoverBackground,
            Color.white
        );

        SetState(
            style.onActive,
            _buttonActiveBackground,
            Color.white
        );

        SetState(
            style.onFocused,
            _buttonBackground,
            normalText
        );

        return style;
    }

    private GUIStyle CreateSelectedCategoryButtonStyle()
    {
        var style = new GUIStyle(CategoryButton)
        {
            fontStyle = FontStyle.Bold
        };

        ApplyBackgroundToAllStates(
            style,
            _accentBackground,
            Color.white
        );

        return style;
    }

    private GUIStyle CreateActionButtonStyle()
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,

            padding = new RectOffset(
                14,
                14,
                8,
                8
            ),

            margin = new RectOffset(
                0,
                0,
                4,
                4
            ),

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14,
            fontStyle = FontStyle.Normal
        };

        Color normalText =
            new(0.88f, 0.90f, 0.94f);

        SetState(
            style.normal,
            _buttonBackground,
            normalText
        );

        SetState(
            style.hover,
            _buttonHoverBackground,
            Color.white
        );

        SetState(
            style.active,
            _buttonActiveBackground,
            Color.white
        );

        SetState(
            style.focused,
            _buttonBackground,
            normalText
        );

        SetState(
            style.onNormal,
            _buttonBackground,
            normalText
        );

        SetState(
            style.onHover,
            _buttonHoverBackground,
            Color.white
        );

        SetState(
            style.onActive,
            _buttonActiveBackground,
            Color.white
        );

        SetState(
            style.onFocused,
            _buttonBackground,
            normalText
        );

        return style;
    }

    private GUIStyle CreateToggleStyle()
    {
        var style = new GUIStyle(GUI.skin.toggle)
        {
            alignment = TextAnchor.MiddleLeft,

            padding = new RectOffset(
                40,
                12,
                8,
                8
            ),

            margin = new RectOffset(
                0,
                0,
                4,
                4
            ),

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14
        };

        Color normalText =
            new(0.86f, 0.88f, 0.92f);

        SetState(
            style.normal,
            _toggleOffBackground,
            normalText
        );

        SetState(
            style.hover,
            _toggleHoverBackground,
            Color.white
        );

        SetState(
            style.active,
            _toggleHoverBackground,
            Color.white
        );

        SetState(
            style.focused,
            _toggleOffBackground,
            normalText
        );

        SetState(
            style.onNormal,
            _accentBackground,
            Color.white
        );

        SetState(
            style.onHover,
            _accentHoverBackground,
            Color.white
        );

        SetState(
            style.onActive,
            _accentHoverBackground,
            Color.white
        );

        SetState(
            style.onFocused,
            _accentBackground,
            Color.white
        );

        return style;
    }

    private GUIStyle CreateThemeButtonStyle()
    {
        return new GUIStyle(ActionButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13
        };
    }

    private GUIStyle CreateSelectedThemeButtonStyle()
    {
        var style = new GUIStyle(ThemeButton)
        {
            fontStyle = FontStyle.Bold
        };

        ApplyBackgroundToAllStates(
            style,
            _accentBackground,
            Color.white
        );

        return style;
    }

    private static GUIStyle CreateTitleStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,

            margin = new RectOffset(
                0,
                0,
                0,
                8
            )
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            Color.white
        );

        return style;
    }

    private static GUIStyle CreateSidebarTitleStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,

            margin = new RectOffset(
                4,
                0,
                0,
                6
            )
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            new Color(
                0.78f,
                0.81f,
                0.87f
            )
        );

        return style;
    }

    private static GUIStyle CreateLabelStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            new Color(
                0.86f,
                0.88f,
                0.92f
            )
        );

        return style;
    }

    private static GUIStyle CreateMutedLabelStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            new Color(
                0.58f,
                0.61f,
                0.68f
            )
        );

        return style;
    }

    private GUIStyle CreateResizeHandleStyle()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            padding = new RectOffset(
                0,
                0,
                0,
                0
            ),

            margin = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        Color normalColor =
            new(0.75f, 0.78f, 0.84f);

        SetState(
            style.normal,
            _resizeBackground,
            normalColor
        );

        SetState(
            style.hover,
            _resizeHoverBackground,
            Color.white
        );

        SetState(
            style.active,
            _resizeHoverBackground,
            Color.white
        );

        SetState(
            style.focused,
            _resizeBackground,
            normalColor
        );

        SetState(
            style.onNormal,
            _resizeBackground,
            normalColor
        );

        SetState(
            style.onHover,
            _resizeHoverBackground,
            Color.white
        );

        SetState(
            style.onActive,
            _resizeHoverBackground,
            Color.white
        );

        SetState(
            style.onFocused,
            _resizeBackground,
            normalColor
        );

        return style;
    }

    private static GUIStyle CreateWindowTitleBoldStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,

            wordWrap = false,
            clipping = TextClipping.Clip,

            stretchWidth = false,
            stretchHeight = false,

            fixedHeight = 20f,

            padding = new RectOffset(
                0,
                0,
                0,
                0
            ),

            margin = new RectOffset(
                0,
                0,
                0,
                0
            ),

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            Color.white
        );

        return style;
    }
    
    private static GUIStyle CreateWindowTitleNormalStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,

            wordWrap = false,
            clipping = TextClipping.Clip,

            stretchWidth = false,
            stretchHeight = false,

            fixedHeight = 20f,

            padding = new RectOffset(
                0,
                0,
                0,
                0
            ),

            margin = new RectOffset(
                0,
                0,
                0,
                0
            ),

            border = new RectOffset(
                0,
                0,
                0,
                0
            ),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            )
        };

        ClearAllStateBackgrounds(style);

        ApplyTextColorToAllStates(
            style,
            new Color(
                0.72f,
                0.74f,
                0.80f
            )
        );

        return style;
    }

    internal void Dispose()
    {
        DestroyTexture(_windowBackground);
        DestroyTexture(_sidebarBackground);
        DestroyTexture(_contentBackground);

        DestroyTexture(_buttonBackground);
        DestroyTexture(_buttonHoverBackground);
        DestroyTexture(_buttonActiveBackground);

        DestroyTexture(_accentBackground);
        DestroyTexture(_accentHoverBackground);

        DestroyTexture(_toggleOffBackground);
        DestroyTexture(_toggleHoverBackground);

        DestroyTexture(_resizeBackground);
        DestroyTexture(_resizeHoverBackground);

        DestroyTexture(SeparatorTexture);
    }

    private static void ApplyBackgroundToAllStates(
        GUIStyle style,
        Texture2D background,
        Color textColor)
    {
        SetState(
            style.normal,
            background,
            textColor
        );

        SetState(
            style.hover,
            background,
            textColor
        );

        SetState(
            style.active,
            background,
            textColor
        );

        SetState(
            style.focused,
            background,
            textColor
        );

        SetState(
            style.onNormal,
            background,
            textColor
        );

        SetState(
            style.onHover,
            background,
            textColor
        );

        SetState(
            style.onActive,
            background,
            textColor
        );

        SetState(
            style.onFocused,
            background,
            textColor
        );
    }

    private static void ApplyTextColorToAllStates(
        GUIStyle style,
        Color textColor)
    {
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        style.focused.textColor = textColor;

        style.onNormal.textColor = textColor;
        style.onHover.textColor = textColor;
        style.onActive.textColor = textColor;
        style.onFocused.textColor = textColor;
    }

    private static void ClearAllStateBackgrounds(
        GUIStyle style)
    {
        style.normal.background = null;
        style.hover.background = null;
        style.active.background = null;
        style.focused.background = null;

        style.onNormal.background = null;
        style.onHover.background = null;
        style.onActive.background = null;
        style.onFocused.background = null;
    }

    private static void SetState(
        GUIStyleState state,
        Texture2D background,
        Color textColor)
    {
        state.background = background;
        state.textColor = textColor;
    }

    private static Texture2D CreateTexture(
        Color color)
    {
        var texture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false
        );

        texture.name =
            "P.E.A.K_MENU Style Texture";

        texture.hideFlags =
            HideFlags.HideAndDontSave;

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Point;

        texture.SetPixel(
            0,
            0,
            color
        );

        texture.Apply(
            updateMipmaps: false,
            makeNoLongerReadable: false
        );

        return texture;
    }

    private static void DestroyTexture(
        Texture2D texture)
    {
        if (texture != null)
        {
            Object.Destroy(texture);
        }
    }

    private readonly struct ThemePalette
    {
        internal Color Window { get; init; }
        internal Color Sidebar { get; init; }
        internal Color Content { get; init; }

        internal Color Button { get; init; }
        internal Color ButtonHover { get; init; }
        internal Color ButtonActive { get; init; }

        internal Color Accent { get; init; }
        internal Color AccentHover { get; init; }

        internal Color ToggleOff { get; init; }
        internal Color ToggleHover { get; init; }

        internal Color Resize { get; init; }
        internal Color Separator { get; init; }

        internal static ThemePalette Create(
            MenuTheme theme)
        {
            return theme switch
            {
                MenuTheme.Ocean =>
                    CreateOcean(),

                MenuTheme.Emerald =>
                    CreateEmerald(),

                MenuTheme.Rose =>
                    CreateRose(),

                MenuTheme.Amber =>
                    CreateAmber(),

                _ =>
                    CreateIris()
            };
        }

        private static ThemePalette CreateIris()
        {
            return CreateBase(
                accent: new Color(
                    0.42f,
                    0.34f,
                    0.78f,
                    1f
                ),

                accentHover: new Color(
                    0.52f,
                    0.43f,
                    0.92f,
                    1f
                ),

                tint: new Color(
                    0.16f,
                    0.13f,
                    0.22f,
                    1f
                )
            );
        }

        private static ThemePalette CreateOcean()
        {
            return CreateBase(
                accent: new Color(
                    0.12f,
                    0.46f,
                    0.76f,
                    1f
                ),

                accentHover: new Color(
                    0.18f,
                    0.58f,
                    0.92f,
                    1f
                ),

                tint: new Color(
                    0.09f,
                    0.16f,
                    0.22f,
                    1f
                )
            );
        }

        private static ThemePalette CreateEmerald()
        {
            return CreateBase(
                accent: new Color(
                    0.10f,
                    0.52f,
                    0.38f,
                    1f
                ),

                accentHover: new Color(
                    0.14f,
                    0.66f,
                    0.48f,
                    1f
                ),

                tint: new Color(
                    0.08f,
                    0.18f,
                    0.15f,
                    1f
                )
            );
        }

        private static ThemePalette CreateRose()
        {
            return CreateBase(
                accent: new Color(
                    0.68f,
                    0.24f,
                    0.42f,
                    1f
                ),

                accentHover: new Color(
                    0.82f,
                    0.31f,
                    0.51f,
                    1f
                ),

                tint: new Color(
                    0.21f,
                    0.10f,
                    0.15f,
                    1f
                )
            );
        }

        private static ThemePalette CreateAmber()
        {
            return CreateBase(
                accent: new Color(
                    0.78f,
                    0.46f,
                    0.10f,
                    1f
                ),

                accentHover: new Color(
                    0.94f,
                    0.59f,
                    0.14f,
                    1f
                ),

                tint: new Color(
                    0.22f,
                    0.15f,
                    0.07f,
                    1f
                )
            );
        }

        private static ThemePalette CreateBase(
            Color accent,
            Color accentHover,
            Color tint)
        {
            return new ThemePalette
            {
                Window = Color.Lerp(
                    new Color(
                        0.07f,
                        0.08f,
                        0.10f,
                        0.98f
                    ),
                    tint,
                    0.18f
                ),

                Sidebar = Color.Lerp(
                    new Color(
                        0.045f,
                        0.05f,
                        0.065f,
                        1f
                    ),
                    tint,
                    0.20f
                ),

                Content = Color.Lerp(
                    new Color(
                        0.10f,
                        0.11f,
                        0.14f,
                        1f
                    ),
                    tint,
                    0.14f
                ),

                Button = Color.Lerp(
                    new Color(
                        0.12f,
                        0.13f,
                        0.16f,
                        1f
                    ),
                    tint,
                    0.15f
                ),

                ButtonHover = Color.Lerp(
                    new Color(
                        0.18f,
                        0.20f,
                        0.25f,
                        1f
                    ),
                    accent,
                    0.16f
                ),

                ButtonActive = Color.Lerp(
                    new Color(
                        0.22f,
                        0.24f,
                        0.30f,
                        1f
                    ),
                    accent,
                    0.24f
                ),

                Accent = accent,
                AccentHover = accentHover,

                ToggleOff = Color.Lerp(
                    new Color(
                        0.13f,
                        0.14f,
                        0.17f,
                        1f
                    ),
                    tint,
                    0.15f
                ),

                ToggleHover = Color.Lerp(
                    new Color(
                        0.19f,
                        0.21f,
                        0.26f,
                        1f
                    ),
                    accent,
                    0.16f
                ),

                Resize = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.20f
                ),

                Separator = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.30f
                )
            };
        }
    }
}