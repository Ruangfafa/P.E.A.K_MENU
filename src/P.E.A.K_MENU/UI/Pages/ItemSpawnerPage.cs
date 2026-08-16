using System;
using System.Collections.Generic;
using System.Linq;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.Input;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class ItemSpawnerPage :
    IMenuPage
{
    private const float ItemRowHeight =
        56f;

    private const float IconSize =
        40f;

    private const float SpawnColumnSpacing =
        6f;

    private const float ManagementHeaderHeight =
        22f;

    private const float ManagementSearchHeight =
        32f;

    private const float ManagementSectionSpacing =
        10f;

    private const float ManagementContentSpacing =
        4f;

    private const float ManagementSearchSpacing =
        6f;

    private const float ManagedItemRowHeight =
        34f;

    private const float AddItemRowHeight =
        42f;

    private const float ManagementScrollbarWidth =
        20f;

    private const float ManagementDragHandleWidth =
        38f;

    private const float ManagementAutoScrollEdge =
        28f;

    private const float ManagementAutoScrollSpeed =
        260f;

    private Vector2 _spawnScroll;
    private Vector2 _manageScroll;
    private Vector2 _addItemsScroll;

    private string _managementStatus =
        string.Empty;

    private bool _managementOpen;

    private string _searchText =
        string.Empty;

    private ItemSpawnEntry? _draggedItem;
    private bool _draggedFromVisible;
    private int _dragControlId;
    private Vector2 _dragMouseScreen;

    private Rect _visibleListScreenRect;
    private float _visibleListContentHeight;
    private int _visibleItemCount;

    private readonly ShortcutRebindControl
        _shortcutRebind = new();

    public string Title =>
        _managementOpen
            ? "物品管理"
            : "物品生成";

    public void Draw(
        MenuStyles styles)
    {
        try
        {
            if (!ItemSpawnRuntime.IsInitialized)
            {
                GUILayout.Label(
                    "ItemSpawn 尚未初始化。",
                    styles.MutedLabel
                );

                GUILayout.Space(8f);

                GUILayout.Label(
                    "请确认当前 Profile 已部署最新 DLL，" +
                    "并检查 LogOutput.log 中的初始化错误。",
                    styles.MutedLabel
                );

                return;
            }

            ItemSpawnRuntime
                .Catalog
                .RefreshIfNeeded();

            if (_managementOpen)
            {
                DrawManagement(styles);
                return;
            }

            DrawSpawnList(styles);
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to draw ItemSpawnerPage: {exception}"
            );

            GUILayout.Label(
                "物品页面加载失败。",
                styles.Label
            );

            GUILayout.Space(8f);

            GUILayout.Label(
                exception.GetType().Name +
                "\n" +
                exception.Message,
                styles.MutedLabel
            );
        }

        _shortcutRebind.CaptureEvent();
    }

    private void DrawSpawnList(
        MenuStyles styles)
    {
        GUILayout.Label(
            "点击物品后，将通过 PEAK 原生网络流程生成并交给本地玩家。",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "生成上一个物品",
            styles.Label,
            GUILayout.Height(38f),
            GUILayout.ExpandWidth(true)
        );

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.SpawnLastItem,
            styles
        );

        GUILayout.EndHorizontal();

        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(
            "左键增加，右键减少",
            styles.MutedLabel,
            GUILayout.Height(32f),
            GUILayout.ExpandWidth(false)
        );

        GUILayout.Space(8f);

        int spawnColumns =
            ItemSpawnConfiguration
                .SpawnColumns;

        Rect columnButtonRect =
            GUILayoutUtility.GetRect(
                64f,
                32f,
                GUILayout.Width(64f),
                GUILayout.Height(32f)
            );

        Event currentEvent =
            Event.current;

        if (currentEvent.type ==
                EventType.MouseDown &&
            columnButtonRect.Contains(
                currentEvent.mousePosition
            ) &&
            (currentEvent.button == 0 ||
             currentEvent.button == 1))
        {
            int nextColumns =
                currentEvent.button == 0
                    ? spawnColumns % 9 + 1
                    : (spawnColumns + 7) % 9 + 1;

            ItemSpawnConfiguration
                .SetSpawnColumns(
                    nextColumns
                );

            currentEvent.Use();
        }

        GUI.Button(
            columnButtonRect,
            $"{spawnColumns}列",
            styles.ActionButton
        );

        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        IReadOnlyList<ItemSpawnEntry>
            entries =
                ItemSpawnRuntime
                    .Catalog
                    .GetVisibleItems();

        /*
         * 只有物品列表使用滚动区域。
         * 底部状态和物品管理按钮始终固定显示。
         */
        _spawnScroll =
            GUILayout.BeginScrollView(
                _spawnScroll,
                false,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        if (entries.Count == 0)
        {
            GUILayout.Label(
                "物品数据库尚未准备好。" +
                "请进入关卡后重新打开菜单。",
                styles.MutedLabel
            );
        }
        else
        {
            DrawSpawnGrid(
                entries,
                styles
            );
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6f);

        GUILayout.Label(
            ItemSpawnRuntime
                .Spawner
                .LastStatus,
            ItemSpawnRuntime
                .Spawner
                .LastSucceeded
                    ? styles.Label
                    : styles.MutedLabel
        );

        GUILayout.Space(4f);

        if (GUILayout.Button(
                "物品管理",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            _managementOpen = true;

            _searchText = string.Empty;
            _managementStatus = string.Empty;

            _manageScroll = Vector2.zero;
            _addItemsScroll = Vector2.zero;
        }
    }

    private static void DrawSpawnGrid(
        IReadOnlyList<ItemSpawnEntry> entries,
        MenuStyles styles)
    {
        int columns =
            ItemSpawnConfiguration
                .SpawnColumns;

        for (int rowStart = 0;
             rowStart < entries.Count;
             rowStart += columns)
        {
            Rect gridRow =
                GUILayoutUtility.GetRect(
                    1f,
                    ItemRowHeight,
                    GUILayout.ExpandWidth(true)
                );

            float itemWidth = Mathf.Max(
                1f,
                (gridRow.width -
                 SpawnColumnSpacing *
                 (columns - 1)) /
                columns
            );

            for (int column = 0;
                 column < columns;
                 column++)
            {
                int index =
                    rowStart + column;

                if (index >= entries.Count)
                {
                    break;
                }

                Rect itemRect = new(
                    gridRow.x +
                    column *
                    (itemWidth +
                     SpawnColumnSpacing),
                    gridRow.y,
                    itemWidth,
                    gridRow.height
                );

                DrawSpawnButton(
                    itemRect,
                    entries[index],
                    styles
                );
            }
        }
    }

    private static void DrawSpawnButton(
        Rect row,
        ItemSpawnEntry entry,
        MenuStyles styles)
    {
        if (GUI.Button(
                row,
                GUIContent.none,
                styles.ActionButton))
        {
            ItemSpawnRuntime
                .Spawner
                .Spawn(entry);
        }

        Rect iconRect = new(
            row.x + 8f,
            row.y +
            (row.height - IconSize) * 0.5f,
            IconSize,
            IconSize
        );

        DrawItemIcon(
            entry,
            iconRect,
            styles
        );

        Rect labelRect = new(
            iconRect.xMax + 10f,
            row.y,
            Mathf.Max(
                0f,
                row.width -
                IconSize -
                30f
            ),
            row.height
        );

        GUI.Label(
            labelRect,
            entry.DisplayName,
            styles.Label
        );
    }

    private static void DrawItemIcon(
        ItemSpawnEntry entry,
        Rect iconRect,
        MenuStyles styles)
    {
        if (entry.Icon is null)
        {
            GUI.Label(
                iconRect,
                "□",
                styles.Label
            );

            return;
        }

        /*
         * 不能只绘制 entry.Icon.texture，
         * 因为 Sprite 可能只是图集中的一小块。
         *
         * CalculateSpriteUv 会计算 Sprite 在原纹理中的
         * 实际区域，避免显示整张图集。
         */
        Rect textureCoordinates =
            CalculateSpriteUv(
                entry.Icon
            );

        GUI.DrawTextureWithTexCoords(
            iconRect,
            entry.Icon.texture,
            textureCoordinates,
            true
        );
    }

    private void DrawManagement(
        MenuStyles styles)
    {
        GUILayout.Label(
            "拖动三横杠可调整顺序，或将下方物品拖到已显示列表。",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

        /*
         * 先取得底部按钮之外的全部弹性空间，
         * 再由 DrawManagementLists 明确按 50% / 50% 分配。
         */
        Rect managementListsRect =
            GUILayoutUtility.GetRect(
                0f,
                float.MaxValue,
                0f,
                float.MaxValue,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        DrawManagementLists(
            managementListsRect,
            styles
        );

        GUILayout.Space(6f);

        if (!string.IsNullOrWhiteSpace(
                _managementStatus))
        {
            GUILayout.Label(
                _managementStatus,
                styles.MutedLabel
            );

            GUILayout.Space(4f);
        }

        /*
         * 底部操作按钮不放入滚动区域，
         * 始终保持可见。
         */
        if (GUILayout.Button(
                "重新扫描游戏物品",
                styles.ActionButton,
                GUILayout.Height(36f)))
        {
            ItemSpawnRuntime
                .Catalog
                .ForceRefresh();

            _manageScroll = Vector2.zero;
            _addItemsScroll = Vector2.zero;

            _managementStatus =
                "已重新扫描游戏物品。";
        }

        if (GUILayout.Button(
                "恢复默认物品和排序",
                styles.ActionButton,
                GUILayout.Height(36f)))
        {
            ItemSpawnRuntime
                .Catalog
                .RestoreDefaults();

            _manageScroll = Vector2.zero;
            _addItemsScroll = Vector2.zero;

            _managementStatus =
                "已恢复默认物品和默认排序。";
        }

        if (GUILayout.Button(
                "返回物品列表",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            _managementOpen = false;
            _searchText = string.Empty;
            _managementStatus = string.Empty;

            _manageScroll = Vector2.zero;
            _addItemsScroll = Vector2.zero;
            CancelManagementDrag();
        }
    }

    private void DrawManagementLists(
        Rect area,
        MenuStyles styles)
    {
        float fixedHeight =
            ManagementHeaderHeight * 2f +
            ManagementSearchHeight +
            ManagementSectionSpacing +
            ManagementContentSpacing * 2f +
            ManagementSearchSpacing;

        float listHeight = Mathf.Max(
            1f,
            (area.height - fixedHeight) * 0.5f
        );

        Rect visibleHeaderRect = new(
            area.x,
            area.y,
            area.width,
            ManagementHeaderHeight
        );

        Rect visibleListRect = new(
            area.x,
            visibleHeaderRect.yMax +
            ManagementContentSpacing,
            area.width,
            listHeight
        );

        Rect addHeaderRect = new(
            area.x,
            visibleListRect.yMax +
            ManagementSectionSpacing,
            area.width,
            ManagementHeaderHeight
        );

        Rect searchRect = new(
            area.x,
            addHeaderRect.yMax +
            ManagementContentSpacing,
            area.width,
            ManagementSearchHeight
        );

        Rect addListRect = new(
            area.x,
            searchRect.yMax +
            ManagementSearchSpacing,
            area.width,
            listHeight
        );

        GUI.Label(
            visibleHeaderRect,
            "已显示物品",
            styles.Label
        );

        DrawVisibleItemsList(
            visibleListRect,
            styles
        );

        GUI.Label(
            addHeaderRect,
            "添加物品",
            styles.Label
        );

        _searchText = GUI.TextField(
            searchRect,
            _searchText
        );

        DrawAddItemsList(
            addListRect,
            styles
        );

        /*
         * 放在两个列表都处理完输入之后绘制，
         * 这样从下方列表开始的拖拽也能在当前帧
         * 立即显示与上方排序相同的插入指示线。
         */
        DrawManagementDropIndicator(
            visibleListRect,
            styles
        );
    }

    private void DrawVisibleItemsList(
        Rect area,
        MenuStyles styles)
    {
        ItemSpawnEntry[] visible =
            ItemSpawnRuntime
                .Catalog
                .GetVisibleItems()
                .ToArray();

        float contentWidth = Mathf.Max(
            1f,
            area.width - ManagementScrollbarWidth
        );

        float contentHeight = Mathf.Max(
            area.height,
            visible.Length * ManagedItemRowHeight
        );

        _visibleListScreenRect =
            ConvertToScreenRect(area);

        _visibleListContentHeight =
            contentHeight;

        _visibleItemCount =
            visible.Length;

        UpdateManagementAutoScroll(
            area.height
        );

        _manageScroll = GUI.BeginScrollView(
            area,
            _manageScroll,
            new Rect(
                0f,
                0f,
                contentWidth,
                contentHeight
            ),
            false,
            true
        );

        if (visible.Length == 0)
        {
            GUI.Label(
                new Rect(
                    0f,
                    0f,
                    contentWidth,
                    ManagedItemRowHeight
                ),
                "当前没有显示任何物品。",
                styles.MutedLabel
            );
        }
        else
        {
            for (int index = 0;
                 index < visible.Length;
                 index++)
            {
                DrawManagedItemRow(
                    new Rect(
                        0f,
                        index * ManagedItemRowHeight,
                        contentWidth,
                        ManagedItemRowHeight
                    ),
                    visible[index],
                    index,
                    styles
                );
            }
        }

        GUI.EndScrollView();
    }

    private void DrawAddItemsList(
        Rect area,
        MenuStyles styles)
    {
        ItemSpawnEntry[] results =
            GetSearchResults();

        float contentWidth = Mathf.Max(
            1f,
            area.width - ManagementScrollbarWidth
        );

        float contentHeight = Mathf.Max(
            area.height,
            results.Length > 0
                ? results.Length * AddItemRowHeight
                : AddItemRowHeight
        );

        _addItemsScroll = GUI.BeginScrollView(
            area,
            _addItemsScroll,
            new Rect(
                0f,
                0f,
                contentWidth,
                contentHeight
            ),
            false,
            true
        );

        if (results.Length == 0)
        {
            string query =
                _searchText.Trim();

            GUI.Label(
                new Rect(
                    0f,
                    0f,
                    contentWidth,
                    AddItemRowHeight
                ),
                string.IsNullOrWhiteSpace(query)
                    ? "所有已发现物品都已添加，" +
                      "或当前没有可添加物品。"
                    : "没有找到匹配的物品。",
                styles.MutedLabel
            );
        }
        else
        {
            for (int index = 0;
                 index < results.Length;
                 index++)
            {
                DrawAddItemRow(
                    new Rect(
                        0f,
                        index * AddItemRowHeight,
                        contentWidth,
                        AddItemRowHeight
                    ),
                    results[index],
                    index,
                    styles
                );
            }
        }

        GUI.EndScrollView();
    }

    private void DrawManagedItemRow(
        Rect row,
        ItemSpawnEntry entry,
        int rowIndex,
        MenuStyles styles)
    {
        DrawManagementRowBackground(
            row,
            rowIndex,
            styles
        );

        Rect fittedIconRect = new(
            row.x + 2f,
            row.y + 4f,
            26f,
            26f
        );

        DrawItemIcon(
            entry,
            fittedIconRect,
            styles
        );

        Rect deleteRect = new(
            row.xMax - 58f,
            row.y,
            58f,
            ManagedItemRowHeight
        );

        Rect dragHandleRect = new(
            deleteRect.x -
            ManagementDragHandleWidth,
            row.y,
            ManagementDragHandleWidth,
            ManagedItemRowHeight
        );

        GUI.Label(
            new Rect(
                row.x + 30f,
                row.y,
                Mathf.Max(
                    0f,
                    dragHandleRect.x -
                    row.x -
                    34f
                ),
                ManagedItemRowHeight
            ),
            entry.DisplayName,
            styles.Label
        );

        DrawManagementDragHandle(
            dragHandleRect,
            entry,
            true,
            styles
        );

        if (GUI.Button(
                deleteRect,
                "删除",
                styles.ActionButton))
        {
            ItemSpawnRuntime
                .Catalog
                .Remove(
                    entry.PrefabName
                );
        }

    }

    private ItemSpawnEntry[] GetSearchResults()
    {
        string query =
            _searchText.Trim();

        IEnumerable<ItemSpawnEntry> candidates =
            ItemSpawnRuntime
                .Catalog
                .AllItems
                .Where(
                    entry =>
                        !ItemSpawnRuntime
                            .Catalog
                            .ContainsVisible(
                                entry.PrefabName
                            )
                )
                .Where(
                    entry =>
                        MatchesSearch(
                            entry,
                            query
                        )
                );

        return candidates.ToArray();
    }

    private void DrawAddItemRow(
        Rect row,
        ItemSpawnEntry entry,
        int rowIndex,
        MenuStyles styles)
    {
        DrawManagementRowBackground(
            row,
            rowIndex,
            styles
        );

        Rect iconRect = new(
            row.x + 2f,
            row.y + 7f,
            28f,
            28f
        );

        DrawItemIcon(
            entry,
            iconRect,
            styles
        );

        Rect buttonRect = new(
            row.xMax - 64f,
            row.y + 4f,
            64f,
            34f
        );

        Rect dragHandleRect = new(
            buttonRect.x -
            ManagementDragHandleWidth,
            row.y + 4f,
            ManagementDragHandleWidth,
            34f
        );

        GUI.Label(
            new Rect(
                row.x + 38f,
                row.y,
                Mathf.Max(
                    0f,
                    dragHandleRect.x -
                    row.x -
                    44f
                ),
                AddItemRowHeight
            ),
            entry.DisplayName,
            styles.Label
        );

        DrawManagementDragHandle(
            dragHandleRect,
            entry,
            false,
            styles
        );

        if (GUI.Button(
                buttonRect,
                "添加",
                styles.ActionButton))
        {
            ItemSpawnRuntime
                .Catalog
                .Add(
                    entry.PrefabName
                );

            _managementStatus =
                $"已添加：{entry.DisplayName}";

            /*
             * 添加后把已显示列表滚动到底部，
             * 方便确认物品已经进入主列表。
             */
            _manageScroll =
                new Vector2(
                    0f,
                    float.MaxValue
                );
        }

    }

    private void DrawManagementDragHandle(
        Rect handleRect,
        ItemSpawnEntry entry,
        bool fromVisible,
        MenuStyles styles)
    {
        int controlId =
            GUIUtility.GetControlID(
                FocusType.Passive,
                handleRect
            );

        Event currentEvent =
            Event.current;

        if (_dragControlId == controlId &&
            _draggedItem is not null)
        {
            _dragMouseScreen =
                GUIUtility.GUIToScreenPoint(
                    currentEvent.mousePosition
                );
        }

        switch (currentEvent.GetTypeForControl(
                    controlId))
        {
            case EventType.MouseDown
                when currentEvent.button == 0 &&
                     handleRect.Contains(
                         currentEvent.mousePosition
                     ):
                _draggedItem = entry;
                _draggedFromVisible =
                    fromVisible;
                _dragControlId = controlId;
                _dragMouseScreen =
                    GUIUtility.GUIToScreenPoint(
                        currentEvent.mousePosition
                    );

                GUIUtility.hotControl =
                    controlId;

                currentEvent.Use();
                break;

            case EventType.MouseDrag
                when GUIUtility.hotControl ==
                     controlId:
                _dragMouseScreen =
                    GUIUtility.GUIToScreenPoint(
                        currentEvent.mousePosition
                    );

                currentEvent.Use();
                break;

            case EventType.MouseUp
                when GUIUtility.hotControl ==
                     controlId:
                _dragMouseScreen =
                    GUIUtility.GUIToScreenPoint(
                        currentEvent.mousePosition
                    );

                CompleteManagementDrag();
                currentEvent.Use();
                break;
        }

        GUI.Box(
            handleRect,
            GUIContent.none,
            styles.ActionButton
        );

        float lineX =
            handleRect.center.x - 7f;

        for (int line = -1;
             line <= 1;
             line++)
        {
            GUI.DrawTexture(
                new Rect(
                    lineX,
                    handleRect.center.y +
                    line * 5f - 1f,
                    14f,
                    2f
                ),
                styles.ManagementDropIndicatorTexture,
                ScaleMode.StretchToFill
            );
        }
    }

    private void CompleteManagementDrag()
    {
        ItemSpawnEntry? draggedItem =
            _draggedItem;

        bool fromVisible =
            _draggedFromVisible;

        if (draggedItem is not null &&
            _visibleListScreenRect.Contains(
                _dragMouseScreen
            ))
        {
            int insertionIndex =
                GetManagementDropIndex();

            bool changed =
                ItemSpawnRuntime
                    .Catalog
                    .PlaceAt(
                        draggedItem.PrefabName,
                        insertionIndex
                    );

            if (changed)
            {
                _managementStatus =
                    fromVisible
                        ? $"已调整：{draggedItem.DisplayName}"
                        : $"已添加：{draggedItem.DisplayName}";
            }
        }

        CancelManagementDrag();
    }

    private void CancelManagementDrag()
    {
        if (GUIUtility.hotControl ==
            _dragControlId)
        {
            GUIUtility.hotControl = 0;
        }

        _draggedItem = null;
        _draggedFromVisible = false;
        _dragControlId = 0;
    }

    private void UpdateManagementAutoScroll(
        float viewportHeight)
    {
        if (_draggedItem is null ||
            !_visibleListScreenRect.Contains(
                _dragMouseScreen
            ))
        {
            return;
        }

        float distanceFromTop =
            _dragMouseScreen.y -
            _visibleListScreenRect.yMin;

        float distanceFromBottom =
            _visibleListScreenRect.yMax -
            _dragMouseScreen.y;

        float direction =
            distanceFromTop <
            ManagementAutoScrollEdge
                ? -1f
                : distanceFromBottom <
                  ManagementAutoScrollEdge
                    ? 1f
                    : 0f;

        if (direction == 0f)
        {
            return;
        }

        float maxScroll = Mathf.Max(
            0f,
            _visibleListContentHeight -
            viewportHeight
        );

        _manageScroll.y = Mathf.Clamp(
            _manageScroll.y +
            direction *
            ManagementAutoScrollSpeed *
            Time.unscaledDeltaTime,
            0f,
            maxScroll
        );
    }

    private void DrawManagementDropIndicator(
        Rect visibleListRect,
        MenuStyles styles)
    {
        if (_draggedItem is null ||
            !_visibleListScreenRect.Contains(
                _dragMouseScreen
            ))
        {
            return;
        }

        int insertionIndex =
            GetManagementDropIndex();

        float indicatorY =
            visibleListRect.y +
            insertionIndex *
            ManagedItemRowHeight -
            _manageScroll.y;

        indicatorY = Mathf.Clamp(
            indicatorY,
            visibleListRect.y + 1.5f,
            visibleListRect.yMax - 1.5f
        );

        GUI.DrawTexture(
            new Rect(
                visibleListRect.x,
                indicatorY - 1.5f,
                Mathf.Max(
                    1f,
                    visibleListRect.width -
                    ManagementScrollbarWidth
                ),
                3f
            ),
            styles.ManagementDropIndicatorTexture,
            ScaleMode.StretchToFill
        );
    }

    private int GetManagementDropIndex()
    {
        float contentY =
            _dragMouseScreen.y -
            _visibleListScreenRect.yMin +
            _manageScroll.y;

        return Mathf.Clamp(
            Mathf.RoundToInt(
                contentY /
                ManagedItemRowHeight
            ),
            0,
            _visibleItemCount
        );
    }

    private static Rect ConvertToScreenRect(
        Rect rect)
    {
        Vector2 screenPosition =
            GUIUtility.GUIToScreenPoint(
                rect.position
            );

        return new Rect(
            screenPosition,
            rect.size
        );
    }

    private static void DrawManagementRowBackground(
        Rect row,
        int rowIndex,
        MenuStyles styles)
    {
        Texture2D? background =
            row.Contains(
                Event.current.mousePosition
            )
                ? styles.ManagementRowHoverTexture
                : rowIndex % 2 == 1
                    ? styles.ManagementRowAlternateTexture
                    : null;

        if (background is not null)
        {
            GUI.DrawTexture(
                row,
                background,
                ScaleMode.StretchToFill
            );
        }
    }

    private static bool MatchesSearch(
        ItemSpawnEntry entry,
        string query)
    {
        if (string.IsNullOrWhiteSpace(
                query))
        {
            return true;
        }

        return
            entry.DisplayName.Contains(
                query,
                StringComparison
                    .OrdinalIgnoreCase
            ) ||
            entry.PrefabName.Contains(
                query,
                StringComparison
                    .OrdinalIgnoreCase
            );
    }

    private static Rect CalculateSpriteUv(
        Sprite sprite)
    {
        Texture2D texture =
            sprite.texture;

        Rect textureRect =
            sprite.textureRect;

        return new Rect(
            textureRect.x /
            texture.width,

            textureRect.y /
            texture.height,

            textureRect.width /
            texture.width,

            textureRect.height /
            texture.height
        );
    }
}
