using System.Collections.Generic;
using P.E.A.K_MENU.Constants;
using P.E.A.K_MENU.UI.Pages;
using P.E.A.K_MENU.Features.Teleport;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class PeakMenuWindow
{
    private const float TitleBarHeight = 38f;
    private const float ResizeHandleMargin = 4f;
    
    private const float CategoryIconSize =
        24f;

    private const float CategoryIconLeftPadding =
        10f;

    private readonly CursorController _cursorController =
        new();

    private readonly Dictionary<MenuCategory, IMenuPage>
        _pages;

    private MenuStyles? _styles;
    private MenuTheme _loadedTheme;

    private bool _isOpen;
    private bool _positionInitialized;
    private bool _closeAfterMouseRelease;
    private bool _disposed;

    private bool _isDragging;
    private bool _isResizing;

    private Vector2 _dragStartMouse;
    private Vector2 _dragStartPosition;

    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;

    private Rect _windowRect;

    private MenuCategory _selectedCategory =
        MenuCategory.ItemSpawner;

    internal bool IsOpen => _isOpen;

    private MenuStyles Styles
    {
        get
        {
            EnsureCurrentTheme();
            return _styles!;
        }
    }

    internal PeakMenuWindow()
    {
        _loadedTheme =
            MenuSettings.Theme;

        _pages =
            new Dictionary<MenuCategory, IMenuPage>
            {
                {
                    MenuCategory.ItemSpawner,
                    new ItemSpawnerPage()
                },
                {
                    MenuCategory.Teleport,
                    new TeleportPage()
                },
                {
                    MenuCategory.Flight,
                    new FlightPage()
                },
                {
                    MenuCategory.Status,
                    new StatusPage()
                },
                {
                    MenuCategory.Settings,
                    new SettingsPage()
                }
            };
    }

    internal void Toggle()
    {
        if (_isOpen)
        {
            Close();
            return;
        }

        Open();
    }

    internal void Open()
    {
        if (_disposed || _isOpen)
        {
            return;
        }

        _isOpen = true;
        _closeAfterMouseRelease = false;

        _isDragging = false;
        _isResizing = false;

        MenuState.IsOpen = true;

        _cursorController.Release();

        ApplyConfiguredWindowSize();
        EnsureCurrentTheme();

        Plugin.Log.LogInfo(
            "P.E.A.K_MENU opened."
        );
    }

    internal void Close()
    {
        if (!_isOpen &&
            !_closeAfterMouseRelease)
        {
            return;
        }

        FinishClose();
    }

    internal void Update()
    {
        if (_closeAfterMouseRelease)
        {
            bool mouseStillPressed =
                UnityEngine.Input.GetMouseButton(0) ||
                UnityEngine.Input.GetMouseButton(1) ||
                UnityEngine.Input.GetMouseButton(2);

            if (!mouseStillPressed)
            {
                FinishClose();
                return;
            }
        }

        if (!_isOpen)
        {
            return;
        }

        RecoverLostPointerRelease();
    }

    internal void LateUpdate()
    {
        if (_disposed ||
            (!_isOpen && !_closeAfterMouseRelease))
        {
            return;
        }

        _cursorController.MaintainReleased();
    }

    internal void Draw()
    {
        if (_disposed || !_isOpen)
        {
            return;
        }

        InitializePosition();
        EnsureCurrentTheme();

        try
        {
            HandleWindowPointerInput();

            _windowRect = GUI.Window(
                ModConstants.WindowId,
                _windowRect,
                DrawWindowContents,
                ModConstants.WindowTitle,
                Styles.Window
            );

            ClampToScreen();
            HandleClickOutside();
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to draw P.E.A.K_MENU: " +
                $"{exception}"
            );

            Close();
        }
    }

    internal void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Close();

        _styles?.Dispose();
        _styles = null;

        _disposed = true;
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
        _styles = new MenuStyles(currentTheme);

        Plugin.Log.LogInfo(
            $"Applied menu theme: {currentTheme}"
        );
    }

    private void BeginCloseAfterMouseRelease()
    {
        if (!_isOpen ||
            _isDragging ||
            _isResizing)
        {
            return;
        }

        _closeAfterMouseRelease = true;

        MenuState.IsOpen = true;

        _cursorController.MaintainReleased();
    }

    private void FinishClose()
    {
        _isOpen = false;
        _closeAfterMouseRelease = false;

        _isDragging = false;
        _isResizing = false;

        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _cursorController.Restore();

        Plugin.Log.LogInfo(
            "P.E.A.K_MENU closed."
        );
    }

    private void DrawWindowContents(
        int windowId)
    {
        DrawCustomTitleBar();

        GUILayout.Space(
            TitleBarHeight - 18f
        );

        try
        {
            GUILayout.BeginHorizontal();

            DrawSidebar();
            DrawCurrentPage();

            GUILayout.EndHorizontal();
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to draw menu contents: {exception}"
            );

            /*
             * 出错后尽可能修复 GUILayout 状态。
             * 即使 ItemSpawn 页面异常，也不会只剩一个空窗口。
             */
            try
            {
                GUILayout.EndHorizontal();
            }
            catch
            {
                // 当前布局组可能尚未成功建立。
            }

            DrawPageError(exception);
        }

        DrawResizeHandle();
    }

    private void DrawCustomTitleBar()
    {
        Rect titleRect = new(
            12f,
            0f,
            Mathf.Max(
                0f,
                _windowRect.width - 24f
            ),
            TitleBarHeight
        );

        GUIContent mainTitleContent =
            new(ModConstants.MainWindowTitle);

        GUIContent authorContent =
            new(ModConstants.WindowAuthorText);

        Vector2 mainTitleSize =
            Styles.WindowTitleBold.CalcSize(
                mainTitleContent
            );

        Vector2 authorSize =
            Styles.WindowTitleNormal.CalcSize(
                authorContent
            );

        float spacing = 14f;

        float totalWidth =
            mainTitleSize.x +
            spacing +
            authorSize.x;

        float startX = Mathf.Max(
            0f,
            titleRect.x +
            (titleRect.width - totalWidth) * 0.5f
        );

        float mainTitleY =
            titleRect.y +
            (titleRect.height - mainTitleSize.y) * 0.5f;

        float authorY =
            titleRect.y +
            (titleRect.height - authorSize.y) * 0.5f;

        GUI.Label(
            new Rect(
                startX,
                mainTitleY,
                mainTitleSize.x + 2f,
                mainTitleSize.y
            ),
            mainTitleContent,
            Styles.WindowTitleBold
        );

        GUI.Label(
            new Rect(
                startX + mainTitleSize.x + spacing,
                authorY,
                authorSize.x + 2f,
                authorSize.y
            ),
            authorContent,
            Styles.WindowTitleNormal
        );
    }

    private void DrawSidebar()
    {
        GUILayout.BeginVertical(
            Styles.Sidebar,
            GUILayout.Width(
                ModConstants.SidebarWidth
            ),
            GUILayout.ExpandHeight(true)
        );

        GUILayout.Label(
            "分类",
            Styles.SidebarTitle
        );

        GUILayout.Space(6f);

        DrawCategoryButton(
            MenuCategory.ItemSpawner,
            "物品生成"
        );

        DrawCategoryButton(
            MenuCategory.Teleport,
            "传送"
        );

        DrawCategoryButton(
            MenuCategory.Flight,
            "飞行"
        );

        DrawCategoryButton(
            MenuCategory.Status,
            "状态"
        );
        
        GUILayout.FlexibleSpace();

        DrawCategoryButton(
            MenuCategory.Settings,
            "设置"
        );

        GUILayout.EndVertical();
    }

    private void DrawCategoryButton(
        MenuCategory category,
        string label)
    {
        bool isSelected =
            _selectedCategory == category;

        GUIStyle style =
            isSelected
                ? Styles.CategoryButtonSelected
                : Styles.CategoryButton;

        Texture2D? icon =
            GetCategoryIcon(
                category
            );

        /*
         * 先让 GUILayout 分配一个固定高度的按钮区域。
         * 不把大尺寸 Texture2D 交给 GUIContent，
         * 避免原图尺寸撑大侧栏。
         */
        Rect buttonRect =
            GUILayoutUtility.GetRect(
                GUIContent.none,
                style,
                GUILayout.Height(
                    ModConstants.CategoryButtonHeight
                ),
                GUILayout.ExpandWidth(true)
            );

        bool clicked =
            GUI.Button(
                buttonRect,
                label,
                style
            );

        if (icon is not null)
        {
            float iconY =
                buttonRect.y +
                (
                    buttonRect.height -
                    CategoryIconSize
                ) *
                0.5f;

            Rect iconRect =
                new(
                    buttonRect.x +
                    CategoryIconLeftPadding,
                    iconY,
                    CategoryIconSize,
                    CategoryIconSize
                );

            /*
             * ScaleMode.ScaleToFit 保持原图比例，
             * 不会把图标拉伸变形。
             */
            GUI.DrawTexture(
                iconRect,
                icon,
                ScaleMode.ScaleToFit,
                alphaBlend: true
            );
        }

        if (!clicked)
        {
            return;
        }

        if (isSelected &&
            category !=
            MenuCategory.Teleport)
        {
            return;
        }

        if (category ==
                MenuCategory.Teleport &&
            !TeleportRuntime.IsInitialized)
        {
            Plugin.Log.LogWarning(
                "TeleportRuntime is not initialized."
            );

            return;
        }

        if (category ==
            MenuCategory.Teleport)
        {
            /*
             * 无论是否已有其他玩家都允许进入页面；
             * 刷新结果和等待提示由页面负责显示。
             */
            TeleportRuntime
                .Service
                .RefreshPlayers();
        }

        _selectedCategory =
            category;

        Plugin.Log.LogInfo(
            $"Selected menu category: {category}"
        );
    }
    
    private static Texture2D?
        GetCategoryIcon(
            MenuCategory category)
    {
        return category switch
        {
            MenuCategory.ItemSpawner =>
                MenuIcons.Item,

            MenuCategory.Teleport =>
                MenuIcons.Teleport,

            MenuCategory.Flight =>
                MenuIcons.Flight,

            MenuCategory.Status =>
                MenuIcons.Status,

            _ =>
                null
        };
    }
    
    private void DrawCurrentPage()
    {
        GUILayout.BeginVertical(
            Styles.Content,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );

        if (!_pages.TryGetValue(
                _selectedCategory,
                out IMenuPage? page))
        {
            GUILayout.Label(
                "页面不存在。",
                Styles.Label
            );

            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label(
            page.Title,
            Styles.Title
        );

        DrawSeparator();

        GUILayout.Space(12f);

        page.Draw(Styles);

        GUILayout.EndVertical();
    }

    private void DrawSeparator()
    {
        Rect separatorRect =
            GUILayoutUtility.GetRect(
                1f,
                1f,
                GUILayout.ExpandWidth(true)
            );

        GUI.DrawTexture(
            separatorRect,
            Styles.SeparatorTexture,
            ScaleMode.StretchToFill
        );
    }

    private void DrawResizeHandle()
    {
        float handleSize =
            ModConstants.ResizeHandleSize;

        Rect localHandleRect = new(
            _windowRect.width
            - handleSize
            - ResizeHandleMargin,
            _windowRect.height
            - handleSize
            - ResizeHandleMargin,
            handleSize,
            handleSize
        );

        GUI.Box(
            localHandleRect,
            "◢",
            Styles.ResizeHandle
        );
    }

    private void HandleWindowPointerInput()
    {
        Event currentEvent =
            Event.current;

        if (currentEvent is null)
        {
            return;
        }

        Vector2 mousePosition =
            currentEvent.mousePosition;

        Rect titleBarRect =
            GetTitleBarScreenRect();

        Rect resizeHandleRect =
            GetResizeHandleScreenRect();

        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                HandlePointerDown(
                    currentEvent,
                    mousePosition,
                    titleBarRect,
                    resizeHandleRect
                );
                break;

            case EventType.MouseDrag:
                HandlePointerDrag(
                    currentEvent,
                    mousePosition
                );
                break;

            case EventType.MouseUp:
                HandlePointerUp(
                    currentEvent
                );
                break;
        }
    }

    private void HandlePointerDown(
        Event currentEvent,
        Vector2 mousePosition,
        Rect titleBarRect,
        Rect resizeHandleRect)
    {
        if (currentEvent.button != 0)
        {
            return;
        }

        if (resizeHandleRect.Contains(
                mousePosition))
        {
            _isResizing = true;
            _isDragging = false;

            _resizeStartMouse =
                mousePosition;

            _resizeStartSize =
                new Vector2(
                    _windowRect.width,
                    _windowRect.height
                );

            currentEvent.Use();
            return;
        }

        if (titleBarRect.Contains(
                mousePosition))
        {
            _isDragging = true;
            _isResizing = false;

            _dragStartMouse =
                mousePosition;

            _dragStartPosition =
                new Vector2(
                    _windowRect.x,
                    _windowRect.y
                );

            currentEvent.Use();
        }
    }

    private void HandlePointerDrag(
        Event currentEvent,
        Vector2 mousePosition)
    {
        if (currentEvent.button != 0)
        {
            return;
        }

        if (_isResizing)
        {
            ResizeWindow(mousePosition);
            currentEvent.Use();
            return;
        }

        if (_isDragging)
        {
            MoveWindow(mousePosition);
            currentEvent.Use();
        }
    }

    private void HandlePointerUp(
        Event currentEvent)
    {
        if (currentEvent.button != 0)
        {
            return;
        }

        if (_isResizing)
        {
            _isResizing = false;

            SaveCurrentWindowSize();

            currentEvent.Use();
            return;
        }

        if (_isDragging)
        {
            _isDragging = false;
            currentEvent.Use();
        }
    }

    private void MoveWindow(
        Vector2 mousePosition)
    {
        Vector2 delta =
            mousePosition - _dragStartMouse;

        float newX =
            _dragStartPosition.x + delta.x;

        float newY =
            _dragStartPosition.y + delta.y;

        _windowRect.x = Mathf.Clamp(
            newX,
            0f,
            Mathf.Max(
                0f,
                Screen.width - _windowRect.width
            )
        );

        _windowRect.y = Mathf.Clamp(
            newY,
            0f,
            Mathf.Max(
                0f,
                Screen.height - _windowRect.height
            )
        );
    }

    private void ResizeWindow(
        Vector2 mousePosition)
    {
        Vector2 delta =
            mousePosition - _resizeStartMouse;

        float maximumWidth =
            Screen.width - _windowRect.x;

        float maximumHeight =
            Screen.height - _windowRect.y;

        float minimumWidth = Mathf.Min(
            ModConstants.MinimumWindowWidth,
            maximumWidth
        );

        float minimumHeight = Mathf.Min(
            ModConstants.MinimumWindowHeight,
            maximumHeight
        );

        _windowRect.width = Mathf.Clamp(
            _resizeStartSize.x + delta.x,
            minimumWidth,
            maximumWidth
        );

        _windowRect.height = Mathf.Clamp(
            _resizeStartSize.y + delta.y,
            minimumHeight,
            maximumHeight
        );
    }

    private Rect GetTitleBarScreenRect()
    {
        return new Rect(
            _windowRect.x,
            _windowRect.y,
            _windowRect.width,
            TitleBarHeight
        );
    }

    private Rect GetResizeHandleScreenRect()
    {
        float handleSize =
            ModConstants.ResizeHandleSize;

        return new Rect(
            _windowRect.x
            + _windowRect.width
            - handleSize
            - ResizeHandleMargin,

            _windowRect.y
            + _windowRect.height
            - handleSize
            - ResizeHandleMargin,

            handleSize,
            handleSize
        );
    }

    private void RecoverLostPointerRelease()
    {
        if (!_isDragging &&
            !_isResizing)
        {
            return;
        }

        if (UnityEngine.Input.GetMouseButton(0))
        {
            return;
        }

        if (_isResizing)
        {
            SaveCurrentWindowSize();
        }

        _isDragging = false;
        _isResizing = false;
    }

    private void SaveCurrentWindowSize()
    {
        MenuSettings.SetWindowSize(
            _windowRect.width,
            _windowRect.height
        );

        Plugin.Log.LogInfo(
            $"Menu resized to " +
            $"{_windowRect.width:0} x " +
            $"{_windowRect.height:0}."
        );
    }

    private void InitializePosition()
    {
        if (_positionInitialized)
        {
            return;
        }

        float width = Mathf.Clamp(
            MenuSettings.WindowWidth,

            Mathf.Min(
                ModConstants.MinimumWindowWidth,
                Screen.width
            ),

            Screen.width
        );

        float height = Mathf.Clamp(
            MenuSettings.WindowHeight,

            Mathf.Min(
                ModConstants.MinimumWindowHeight,
                Screen.height
            ),

            Screen.height
        );

        float x =
            Screen.width
            - width
            - ModConstants.WindowMargin;

        float y =
            ModConstants.WindowTop;

        _windowRect = new Rect(
            Mathf.Max(0f, x),
            Mathf.Max(0f, y),
            width,
            height
        );

        _positionInitialized = true;
    }

    private void ApplyConfiguredWindowSize()
    {
        if (!_positionInitialized)
        {
            return;
        }

        float maximumWidth =
            Screen.width - _windowRect.x;

        float maximumHeight =
            Screen.height - _windowRect.y;

        float minimumWidth = Mathf.Min(
            ModConstants.MinimumWindowWidth,
            maximumWidth
        );

        float minimumHeight = Mathf.Min(
            ModConstants.MinimumWindowHeight,
            maximumHeight
        );

        _windowRect.width = Mathf.Clamp(
            MenuSettings.WindowWidth,
            minimumWidth,
            maximumWidth
        );

        _windowRect.height = Mathf.Clamp(
            MenuSettings.WindowHeight,
            minimumHeight,
            maximumHeight
        );

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        float maximumWidth = Screen.width;

        float maximumHeight = Screen.height;

        float minimumWidth = Mathf.Min(
            ModConstants.MinimumWindowWidth,
            maximumWidth
        );

        float minimumHeight = Mathf.Min(
            ModConstants.MinimumWindowHeight,
            maximumHeight
        );

        _windowRect.width = Mathf.Clamp(
            _windowRect.width,
            minimumWidth,
            maximumWidth
        );

        _windowRect.height = Mathf.Clamp(
            _windowRect.height,
            minimumHeight,
            maximumHeight
        );

        _windowRect.x = Mathf.Clamp(
            _windowRect.x,
            0f,
            Mathf.Max(
                0f,
                Screen.width - _windowRect.width
            )
        );

        _windowRect.y = Mathf.Clamp(
            _windowRect.y,
            0f,
            Mathf.Max(
                0f,
                Screen.height - _windowRect.height
            )
        );
    }

    private void HandleClickOutside()
    {
        if (_isDragging ||
            _isResizing ||
            _closeAfterMouseRelease)
        {
            return;
        }

        Event currentEvent =
            Event.current;

        if (currentEvent is null ||
            currentEvent.type !=
            EventType.MouseDown ||
            currentEvent.button != 0)
        {
            return;
        }

        if (_windowRect.Contains(
                currentEvent.mousePosition))
        {
            return;
        }

        BeginCloseAfterMouseRelease();
        currentEvent.Use();
    }
    
    private void DrawPageError(
        System.Exception exception)
    {
        Rect errorRect = new(
            20f,
            TitleBarHeight + 12f,
            Mathf.Max(
                100f,
                _windowRect.width - 40f
            ),
            Mathf.Max(
                100f,
                _windowRect.height
                - TitleBarHeight
                - 32f
            )
        );

        GUI.Label(
            errorRect,
            "菜单页面绘制失败。\n\n" +
            exception.GetType().Name +
            "\n" +
            exception.Message +
            "\n\n请查看 BepInEx/LogOutput.log。",
            Styles.Label
        );
    }
}
