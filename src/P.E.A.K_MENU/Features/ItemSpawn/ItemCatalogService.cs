using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zorro.Core;

namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// 负责读取 PEAK 物品数据库、管理用户显示列表和排序。
/// </summary>
internal sealed class ItemCatalogService :
    IDisposable
{
    private readonly ItemIconResolver
        _iconResolver = new();

    private readonly List<ItemSpawnEntry>
        _allItems = new();

    private readonly List<string>
        _visibleNames = new();

    private int _loadedDatabaseId;

    internal IReadOnlyList<ItemSpawnEntry>
        AllItems => _allItems;

    /// <summary>
    /// 检查游戏物品数据库是否已经准备好。
    /// 数据库发生变化时重新读取。
    /// </summary>
    internal void RefreshIfNeeded(
        bool force = false)
    {
        ItemDatabase? database =
            SingletonAsset<ItemDatabase>.Instance;

        if (database is null ||
            database.Objects is null)
        {
            return;
        }

        int databaseId =
            database.GetInstanceID();

        if (!force &&
            _loadedDatabaseId == databaseId &&
            _allItems.Count > 0)
        {
            return;
        }

        LoadDatabase(
            database,
            databaseId
        );
    }

    /// <summary>
    /// 强制重新扫描物品数据库。
    /// </summary>
    internal void ForceRefresh()
    {
        RefreshIfNeeded(
            force: true
        );
    }

    /// <summary>
    /// 获取用户当前选择显示的物品，
    /// 返回顺序与配置保存顺序一致。
    /// </summary>
    internal IReadOnlyList<ItemSpawnEntry>
        GetVisibleItems()
    {
        RefreshIfNeeded();

        var result =
            new List<ItemSpawnEntry>();

        foreach (string name
                 in _visibleNames)
        {
            ItemSpawnEntry? entry =
                FindByPrefabName(name);

            if (entry is not null)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    internal bool ContainsVisible(
        string prefabName)
    {
        return _visibleNames.Contains(
            prefabName,
            StringComparer.OrdinalIgnoreCase
        );
    }

    internal void Add(
        string prefabName)
    {
        if (ContainsVisible(prefabName))
        {
            return;
        }

        ItemSpawnEntry? entry =
            FindByPrefabName(prefabName);

        if (entry is null)
        {
            return;
        }

        _visibleNames.Add(
            entry.PrefabName
        );

        Save();

        Plugin.Log.LogInfo(
            $"Added item to ItemSpawn list: " +
            $"{entry.PrefabName}"
        );
    }

    /// <summary>
    /// 将物品插入指定的列表间隙。
    /// 已显示的物品会移动到该位置，
    /// 未显示的物品会直接添加到该位置。
    /// </summary>
    internal bool PlaceAt(
        string prefabName,
        int insertionIndex)
    {
        ItemSpawnEntry? entry =
            FindByPrefabName(prefabName);

        if (entry is null)
        {
            return false;
        }

        int currentIndex =
            _visibleNames.FindIndex(
                name =>
                    string.Equals(
                        name,
                        prefabName,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );

        int targetIndex = Math.Max(
            0,
            Math.Min(
                _visibleNames.Count,
                insertionIndex
            )
        );

        if (currentIndex >= 0)
        {
            if (currentIndex < targetIndex)
            {
                targetIndex--;
            }

            if (currentIndex == targetIndex)
            {
                return false;
            }

            _visibleNames.RemoveAt(
                currentIndex
            );
        }

        targetIndex = Math.Min(
            targetIndex,
            _visibleNames.Count
        );

        _visibleNames.Insert(
            targetIndex,
            entry.PrefabName
        );

        Save();

        Plugin.Log.LogInfo(
            $"Placed item in ItemSpawn list: " +
            $"{entry.PrefabName} at {targetIndex}"
        );

        return true;
    }

    internal void Remove(
        string prefabName)
    {
        int removedCount =
            _visibleNames.RemoveAll(
                name =>
                    string.Equals(
                        name,
                        prefabName,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );

        if (removedCount <= 0)
        {
            return;
        }

        Save();

        Plugin.Log.LogInfo(
            $"Removed item from ItemSpawn list: " +
            $"{prefabName}"
        );
    }

    /// <summary>
    /// direction 为 -1 时向上移动，
    /// direction 为 1 时向下移动。
    /// </summary>
    internal void Move(
        string prefabName,
        int direction)
    {
        int index =
            _visibleNames.FindIndex(
                name =>
                    string.Equals(
                        name,
                        prefabName,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );

        if (index < 0)
        {
            return;
        }

        int target =
            index + direction;

        if (target < 0 ||
            target >= _visibleNames.Count)
        {
            return;
        }

        string currentValue =
            _visibleNames[index];

        _visibleNames[index] =
            _visibleNames[target];

        _visibleNames[target] =
            currentValue;

        Save();
    }

    /// <summary>
    /// 恢复 ItemSpawnDefaults 中定义的
    /// 默认物品以及默认排序。
    /// </summary>
    internal void RestoreDefaults()
    {
        _visibleNames.Clear();

        foreach (string preferred
                 in ItemSpawnDefaults
                     .PreferredPrefabNames)
        {
            ItemSpawnEntry? exact =
                FindByPrefabName(
                    preferred
                );

            ItemSpawnEntry? match =
                exact ??
                _allItems.FirstOrDefault(
                    entry =>
                        entry.PrefabName.Contains(
                            preferred,
                            StringComparison
                                .OrdinalIgnoreCase
                        ) ||
                        entry.DisplayName.Contains(
                            preferred,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                );

            if (match is null)
            {
                continue;
            }

            if (ContainsVisible(
                    match.PrefabName))
            {
                continue;
            }

            _visibleNames.Add(
                match.PrefabName
            );
        }

        /*
         * 如果默认候选没有匹配到任何物品，
         * 则显示数据库排序后的前几项，
         * 避免生成页面完全为空。
         */
        if (_visibleNames.Count == 0)
        {
            IEnumerable<string> fallback =
                _allItems
                    .Take(
                        ItemSpawnDefaults
                            .FallbackItemCount
                    )
                    .Select(
                        entry =>
                            entry.PrefabName
                    );

            _visibleNames.AddRange(
                fallback
            );
        }

        Save();

        Plugin.Log.LogInfo(
            $"Restored default ItemSpawn list: " +
            $"{_visibleNames.Count} items."
        );
    }

    public void Dispose()
    {
        _iconResolver.Dispose();

        _allItems.Clear();
        _visibleNames.Clear();

        _loadedDatabaseId = 0;
    }

    private void LoadDatabase(
        ItemDatabase database,
        int databaseId)
    {
        _allItems.Clear();

        foreach (Item? item
                 in database.Objects)
        {
            if (item is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    item.name))
            {
                continue;
            }

            string displayName =
                ResolveDisplayName(item);

            Sprite? icon =
                _iconResolver.Resolve(item);

            var entry =
                new ItemSpawnEntry(
                    item,
                    displayName,
                    icon
                );

            /*
             * 防止数据库中出现同名重复项。
             */
            bool alreadyExists =
                _allItems.Any(
                    existing =>
                        string.Equals(
                            existing.PrefabName,
                            entry.PrefabName,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                );

            if (alreadyExists)
            {
                continue;
            }

            _allItems.Add(entry);
        }

        _allItems.Sort(
            (left, right) =>
                string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison
                        .OrdinalIgnoreCase
                )
        );

        _loadedDatabaseId =
            databaseId;

        LoadConfiguredOrder();

        Plugin.Log.LogInfo(
            $"Item catalog loaded: " +
            $"{_allItems.Count} items."
        );
    }

    private void LoadConfiguredOrder()
    {
        _visibleNames.Clear();

        IReadOnlyList<string> configured =
            ItemSpawnConfiguration
                .LoadVisibleItemNames();

        foreach (string name
                 in configured)
        {
            if (FindByPrefabName(name)
                is null)
            {
                continue;
            }

            if (ContainsVisible(name))
            {
                continue;
            }

            _visibleNames.Add(name);
        }

        if (_visibleNames.Count == 0)
        {
            RestoreDefaults();
        }
    }

    private ItemSpawnEntry?
        FindByPrefabName(
            string prefabName)
    {
        return _allItems.FirstOrDefault(
            entry =>
                string.Equals(
                    entry.PrefabName,
                    prefabName,
                    StringComparison
                        .OrdinalIgnoreCase
                )
        );
    }

    private void Save()
    {
        ItemSpawnConfiguration
            .SaveVisibleItemNames(
                _visibleNames
            );
    }

    private static string ResolveDisplayName(
        Item item)
    {
        /*
         * 第一版先使用 prefab 名称。
         *
         * 后续确认 PEAK 当前版本的本地化接口后，
         * 可以在这里改成游戏本地化显示名称。
         */
        return item.name ??
               "Unknown Item";
    }
}
