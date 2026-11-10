using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class MenuStyles
{
    private static readonly Color AccentText = new Color32(0x07, 0x10, 0x19, 0xFF);
    internal GUIStyle Window { get; }
    internal GUIStyle Sidebar { get; }
    internal GUIStyle Content { get; }

    internal GUIStyle CategoryButton { get; }
    internal GUIStyle CategoryButtonSelected { get; }

    internal GUIStyle ActionButton { get; }
    internal GUIStyle ConfirmationButton { get; }
    internal GUIStyle Toggle { get; }
    internal GUIStyle TextField { get; }

    internal GUIStyle ThemeButton { get; }
    internal GUIStyle ThemeButtonSelected { get; }

    internal GUIStyle Title { get; }
    internal GUIStyle SectionTitle { get; }
    internal GUIStyle SidebarTitle { get; }
    internal GUIStyle Label { get; }
    internal GUIStyle MutedLabel { get; }
    internal GUIStyle NoticeLabel { get; }
    internal GUIStyle GiftBadge { get; }

    internal GUIStyle ResizeHandle { get; }

    internal GUIStyle WindowTitleBold { get; }
    internal GUIStyle WindowTitleNormal { get; }

    internal Texture2D SeparatorTexture { get; }
    internal Texture2D SectionDividerTexture { get; }
    internal Texture2D SectionAccentTexture { get; }
    internal Texture2D MasterSwitchOnTexture { get; }
    internal Texture2D MasterSwitchOffTexture { get; }
    internal Texture2D MasterSwitchKnobTexture { get; }
    internal Texture2D ManagementRowAlternateTexture { get; }
    internal Texture2D ManagementRowHoverTexture { get; }
    internal Texture2D ManagementDropIndicatorTexture { get; }

    private readonly Texture2D _windowBackground;
    private readonly Texture2D _sidebarBackground;
    private readonly Texture2D _contentBackground;

    private readonly Texture2D _buttonBackground;
    private readonly Texture2D _buttonHoverBackground;
    private readonly Texture2D _buttonActiveBackground;
    private readonly Texture2D _inputFocusBackground;
    private readonly Texture2D _confirmationBackground;

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
            CreateRoundedTexture(palette.Window, palette.Line, 18, 1, 64);

        _sidebarBackground =
            CreateRoundedTexture(palette.Sidebar, palette.Line, 14, 1, 48);

        _contentBackground =
            CreateRoundedTexture(palette.Content, palette.Line, 18, 1, 64);

        _buttonBackground =
            CreateRoundedTexture(palette.Button, palette.Line, 11, 1, 40);

        _buttonHoverBackground =
            CreateRoundedTexture(palette.ButtonHover, palette.LineHover, 11, 1, 40);

        _buttonActiveBackground =
            CreateRoundedTexture(palette.ButtonActive, palette.Accent, 11, 1, 40);

        _inputFocusBackground =
            CreateRoundedTexture(palette.Button, palette.Accent, 11, 2, 40);

        _accentBackground =
            CreateGradientRoundedTexture(palette.Accent, palette.AccentSecondary, 11, 0, 40);

        _accentHoverBackground =
            CreateGradientRoundedTexture(palette.AccentHover, palette.AccentSecondaryHover, 11, 0, 40);

        _toggleOffBackground =
            CreateRoundedTexture(palette.ToggleOff, palette.Line, 11, 1, 40);

        _toggleHoverBackground =
            CreateRoundedTexture(palette.ToggleHover, palette.LineHover, 11, 1, 40);

        _resizeBackground =
            CreateTexture(palette.Resize);

        _resizeHoverBackground =
            CreateTexture(palette.AccentHover);

        SeparatorTexture = CreateTexture(palette.Separator);
        SectionDividerTexture = CreateGradientRoundedTexture(palette.Accent,
            palette.AccentSecondary, 2, 0, 64);
        SectionAccentTexture = CreateGradientRoundedTexture(palette.Accent,
            palette.AccentSecondary, 2, 0, 64);
        MasterSwitchOnTexture = CreateRoundedTexture(palette.AccentSecondary,
            palette.AccentSecondaryHover, 5, 1, 40);
        MasterSwitchOffTexture = CreateRoundedTexture(palette.ToggleOff,
            palette.LineHover, 5, 1, 40);
        MasterSwitchKnobTexture = CreateRoundedTexture(Color.white,
            new Color32(0xD8, 0xE0, 0xEC, 0xFF), 4, 1, 40);

        ManagementRowAlternateTexture =
            CreateTexture(
                Color.Lerp(palette.Content, palette.Button, 0.5f)
            );

        ManagementRowHoverTexture =
            CreateTexture(
                Color.Lerp(palette.Content, palette.ButtonHover, 0.72f)
            );

        ManagementDropIndicatorTexture =
            CreateTexture(palette.AccentHover);

        Window = CreateWindowStyle();
        Sidebar = CreateSidebarStyle();
        Content = CreateContentStyle();

        CategoryButton =
            CreateCategoryButtonStyle();

        CategoryButtonSelected =
            CreateSelectedCategoryButtonStyle();

        ActionButton =
            CreateActionButtonStyle();
        _confirmationBackground = CreateRoundedTexture(new Color32(0xF7, 0x54, 0x64, 0xFF),
            new Color32(0xFB, 0x71, 0x85, 0xFF), 11, 1, 40);
        ConfirmationButton = new GUIStyle(ActionButton);
        ApplyBackgroundToAllStates(ConfirmationButton, _confirmationBackground, Color.white);

        Toggle =
            CreateToggleStyle();

        TextField =
            CreateTextFieldStyle();

        ThemeButton =
            CreateThemeButtonStyle();

        ThemeButtonSelected =
            CreateSelectedThemeButtonStyle();

        Title =
            CreateTitleStyle();

        SectionTitle =
            CreateSectionTitleStyle();

        SidebarTitle =
            CreateSidebarTitleStyle();

        Label =
            CreateLabelStyle();

        MutedLabel =
            CreateMutedLabelStyle();

        NoticeLabel = new GUIStyle(Label) { fontStyle = FontStyle.Bold, wordWrap = true };
        NoticeLabel.normal.textColor = Color.Lerp(palette.AccentHover, Color.white, 0.4f);

        GiftBadge =
            CreateGiftBadgeStyle();

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

            border = new RectOffset(18, 18, 18, 18),

            padding = new RectOffset(
                16,
                16,
                34,
                16
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
            border = new RectOffset(14, 14, 14, 14),

            padding = new RectOffset(
                12,
                12,
                14,
                14
            ),

            margin = new RectOffset(
                0,
                12,
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
            border = new RectOffset(18, 18, 18, 18),

            padding = new RectOffset(
                20,
                20,
                18,
                18
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
            alignment =
                TextAnchor.MiddleCenter,

            imagePosition =
                ImagePosition.TextOnly,

            padding = new RectOffset(
                10,
                10,
                5,
                5
            ),

            margin = new RectOffset(
                0,
                0,
                3,
                3
            ),

            border = new RectOffset(11, 11, 11, 11),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        Color normalText = new Color32(0xED, 0xF2, 0xFF, 0xFF);

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
            AccentText
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

            border = new RectOffset(11, 11, 11, 11),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        Color normalText = new Color32(0xED, 0xF2, 0xFF, 0xFF);

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

            border = new RectOffset(11, 11, 11, 11),

            overflow = new RectOffset(
                0,
                0,
                0,
                0
            ),

            fontSize = 14
        };

        Color normalText = new Color32(0xED, 0xF2, 0xFF, 0xFF);

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
            AccentText
        );

        SetState(
            style.onHover,
            _accentHoverBackground,
            AccentText
        );

        SetState(
            style.onActive,
            _accentHoverBackground,
            AccentText
        );

        SetState(
            style.onFocused,
            _accentBackground,
            AccentText
        );

        return style;
    }

    private GUIStyle CreateTextFieldStyle()
    {
        var style = new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(13, 13, 8, 8),
            border = new RectOffset(11, 11, 11, 11),
            fontSize = 14
        };

        Color text = new Color32(0xED, 0xF2, 0xFF, 0xFF);
        SetState(style.normal, _buttonBackground, text);
        SetState(style.hover, _buttonHoverBackground, Color.white);
        SetState(style.active, _buttonActiveBackground, Color.white);
        SetState(style.focused, _inputFocusBackground, Color.white);
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
            AccentText
        );

        return style;
    }

    private static GUIStyle CreateTitleStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
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

    private static GUIStyle CreateSectionTitleStyle()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            clipping = TextClipping.Clip,
            fixedHeight = 0f,
            stretchWidth = true,
            stretchHeight = true,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        ClearAllStateBackgrounds(style);
        ApplyTextColorToAllStates(style, Color.white);
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
            new Color32(0x98, 0xA6, 0xBE, 0xFF)
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
            new Color32(0xED, 0xF2, 0xFF, 0xFF)
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
            new Color32(0x98, 0xA6, 0xBE, 0xFF)
        );

        return style;
    }

    private GUIStyle CreateGiftBadgeStyle()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(6, 6, 2, 2),
            margin = new RectOffset(6, 2, 1, 1)
        };

        style.border = new RectOffset(11, 11, 11, 11);

        ApplyBackgroundToAllStates(style, _accentBackground, AccentText);
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
            fontSize = 17,
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
            new Color32(0x98, 0xA6, 0xBE, 0xFF)
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
        DestroyTexture(_inputFocusBackground);
        DestroyTexture(_confirmationBackground);

        DestroyTexture(_accentBackground);
        DestroyTexture(_accentHoverBackground);

        DestroyTexture(_toggleOffBackground);
        DestroyTexture(_toggleHoverBackground);

        DestroyTexture(_resizeBackground);
        DestroyTexture(_resizeHoverBackground);

        DestroyTexture(SeparatorTexture);
        DestroyTexture(SectionDividerTexture);
        DestroyTexture(SectionAccentTexture);
        DestroyTexture(MasterSwitchOnTexture);
        DestroyTexture(MasterSwitchOffTexture);
        DestroyTexture(MasterSwitchKnobTexture);
        DestroyTexture(ManagementRowAlternateTexture);
        DestroyTexture(ManagementRowHoverTexture);
        DestroyTexture(ManagementDropIndicatorTexture);
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

    private static Texture2D CreateRoundedTexture(Color fill, Color line, int radius,
        int lineWidth, int size) => CreateRoundedTexture(fill, fill, line, radius, lineWidth, size);

    private static Texture2D CreateGradientRoundedTexture(Color left, Color right,
        int radius, int lineWidth, int size) =>
        CreateRoundedTexture(left, right, Color.clear, radius, lineWidth, size);

    private static Texture2D CreateRoundedTexture(Color left, Color right, Color line,
        int radius, int lineWidth, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "P.E.A.K_MENU Rounded Style Texture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float edgeX = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0f;
            float edgeY = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0f;
            float distance = Mathf.Sqrt(edgeX * edgeX + edgeY * edgeY);
            float alpha = Mathf.Clamp01(radius + 0.5f - distance);
            Color color = Color.Lerp(left, right, x / (float)(size - 1));
            if (lineWidth > 0 && distance > radius - lineWidth - 0.5f)
                color = Color.Lerp(color, line, Mathf.Clamp01(distance - (radius - lineWidth - 0.5f)));
            color.a *= alpha;
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, false);
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
        internal Color AccentSecondary { get; init; }
        internal Color AccentSecondaryHover { get; init; }
        internal Color Line { get; init; }
        internal Color LineHover { get; init; }

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
                Window = new Color32(0x09, 0x0C, 0x12, 0xFC),
                Sidebar = new Color32(0x0D, 0x13, 0x1D, 0xFA),
                Content = new Color32(0x11, 0x17, 0x22, 0xFA),
                Button = new Color32(0x0B, 0x10, 0x19, 0xFF),
                ButtonHover = new Color32(0x14, 0x1C, 0x29, 0xFF),
                ButtonActive = new Color32(0x19, 0x24, 0x35, 0xFF),

                Accent = new Color32(0x7D, 0xD3, 0xFC, 0xFF),
                AccentHover = new Color32(0xA5, 0xE3, 0xFD, 0xFF),
                AccentSecondary = accent,
                AccentSecondaryHover = accentHover,
                Line = new Color32(0x26, 0x30, 0x44, 0xFF),
                LineHover = new Color32(0x62, 0x71, 0x8A, 0xFF),

                ToggleOff = new Color32(0x0B, 0x10, 0x19, 0xFF),
                ToggleHover = new Color32(0x14, 0x1C, 0x29, 0xFF),

                Resize = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.20f
                ),

                Separator = new Color32(0x26, 0x30, 0x44, 0xFF)
            };
        }
    }
}
