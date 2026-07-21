using System;
using System.Collections.Generic;
using System.Linq;
using P.E.A.K_MENU.Features.ItemSpawn;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class ItemSpawnerPage :
    IMenuPage
{
    private const float ItemRowHeight =
        56f;

    private const float IconSize =
        40f;

    private const float VisibleItemsListHeight =
        200f;

    private const float AddItemsListHeight =
        180f;

    private const int MaximumSearchResults =
        100;

    private Vector2 _spawnScroll;
    private Vector2 _manageScroll;
    private Vector2 _addItemsScroll;

    private string _managementStatus =
        string.Empty;

    private bool _managementOpen;

    private string _searchText =
        string.Empty;

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
    }

    private void DrawSpawnList(
        MenuStyles styles)
    {
        GUILayout.Label(
            "点击物品后，将通过 PEAK 原生网络流程生成并交给本地玩家。",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

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
            foreach (ItemSpawnEntry entry
                     in entries)
            {
                DrawSpawnButton(
                    entry,
                    styles
                );
            }
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

    private static void DrawSpawnButton(
        ItemSpawnEntry entry,
        MenuStyles styles)
    {
        Rect row =
            GUILayoutUtility.GetRect(
                1f,
                ItemRowHeight,
                GUILayout.ExpandWidth(true)
            );

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
        "添加、删除或调整主列表的显示顺序。",
        styles.MutedLabel
    );

    GUILayout.Space(8f);

    /*
     * 已显示物品区域。
     */
    GUILayout.Label(
        "已显示物品",
        styles.Label
    );

    GUILayout.Space(4f);

    _manageScroll =
        GUILayout.BeginScrollView(
            _manageScroll,
            false,
            true,
            GUILayout.Height(
                VisibleItemsListHeight
            ),
            GUILayout.ExpandWidth(true)
        );

    IReadOnlyList<ItemSpawnEntry> visible =
        ItemSpawnRuntime
            .Catalog
            .GetVisibleItems();

    if (visible.Count == 0)
    {
        GUILayout.Label(
            "当前没有显示任何物品。",
            styles.MutedLabel
        );
    }
    else
    {
        /*
         * 使用快照，避免点击删除或移动时
         * 修改正在遍历的集合。
         */
        ItemSpawnEntry[] visibleSnapshot =
            visible.ToArray();

        foreach (ItemSpawnEntry entry
                 in visibleSnapshot)
        {
            DrawManagedItemRow(
                entry,
                styles
            );
        }
    }

    GUILayout.EndScrollView();

    GUILayout.Space(10f);

    /*
     * 添加物品区域。
     */
    GUILayout.Label(
        "添加物品",
        styles.Label
    );

    GUILayout.Space(4f);

    _searchText =
        GUILayout.TextField(
            _searchText,
            GUILayout.Height(32f),
            GUILayout.ExpandWidth(true)
        );

    GUILayout.Space(6f);

    /*
     * 添加列表拥有独立的滚动区域。
     */
    _addItemsScroll =
        GUILayout.BeginScrollView(
            _addItemsScroll,
            false,
            true,
            GUILayout.Height(
                AddItemsListHeight
            ),
            GUILayout.ExpandWidth(true)
        );

    DrawSearchResults(styles);

    GUILayout.EndScrollView();

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
    }
}

    private static void DrawManagedItemRow(
        ItemSpawnEntry entry,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal();

        Rect iconRect =
            GUILayoutUtility.GetRect(
                30f,
                34f,
                GUILayout.Width(30f),
                GUILayout.Height(34f)
            );

        Rect fittedIconRect = new(
            iconRect.x + 2f,
            iconRect.y + 4f,
            26f,
            26f
        );

        DrawItemIcon(
            entry,
            fittedIconRect,
            styles
        );

        GUILayout.Label(
            entry.DisplayName,
            styles.Label,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(34f)
        );

        if (GUILayout.Button(
                "↑",
                styles.ActionButton,
                GUILayout.Width(38f),
                GUILayout.Height(34f)))
        {
            ItemSpawnRuntime
                .Catalog
                .Move(
                    entry.PrefabName,
                    -1
                );
        }

        if (GUILayout.Button(
                "↓",
                styles.ActionButton,
                GUILayout.Width(38f),
                GUILayout.Height(34f)))
        {
            ItemSpawnRuntime
                .Catalog
                .Move(
                    entry.PrefabName,
                    1
                );
        }

        if (GUILayout.Button(
                "删除",
                styles.ActionButton,
                GUILayout.Width(58f),
                GUILayout.Height(34f)))
        {
            ItemSpawnRuntime
                .Catalog
                .Remove(
                    entry.PrefabName
                );
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSearchResults(
        MenuStyles styles)
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
                )
                .Take(
                    MaximumSearchResults
                );

        ItemSpawnEntry[] results =
            candidates.ToArray();

        if (results.Length == 0)
        {
            GUILayout.Label(
                string.IsNullOrWhiteSpace(query)
                    ? "所有已发现物品都已添加，" +
                      "或当前没有可添加物品。"
                    : "没有找到匹配的物品。",
                styles.MutedLabel
            );

            return;
        }

        foreach (ItemSpawnEntry entry
                 in results)
        {
            DrawAddItemRow(
                entry,
                styles
            );
        }
    }

    private void DrawAddItemRow(
        ItemSpawnEntry entry,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal(
            GUILayout.Height(42f)
        );

        Rect iconArea =
            GUILayoutUtility.GetRect(
                32f,
                42f,
                GUILayout.Width(32f),
                GUILayout.Height(42f)
            );

        Rect iconRect = new(
            iconArea.x + 2f,
            iconArea.y + 7f,
            28f,
            28f
        );

        DrawItemIcon(
            entry,
            iconRect,
            styles
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            entry.DisplayName,
            styles.Label,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(42f)
        );

        if (GUILayout.Button(
                "添加",
                styles.ActionButton,
                GUILayout.Width(64f),
                GUILayout.Height(34f)))
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

        GUILayout.EndHorizontal();
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