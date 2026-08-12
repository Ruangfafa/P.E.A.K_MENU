using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// 负责保存用户当前显示的物品以及显示顺序。
/// </summary>
internal static class ItemSpawnConfiguration
{
    private static ConfigEntry<string>? _visibleItems;
    private static ConfigEntry<int>? _spawnColumns;

    internal static int SpawnColumns =>
        Math.Max(
            1,
            Math.Min(
                9,
                _spawnColumns?.Value ?? 4
            )
        );

    internal static void Initialize(
        ConfigFile config)
    {
        _visibleItems = config.Bind(
            "ItemSpawn",
            "VisibleItems",
            string.Empty,
            "物品生成页面显示的物品 prefab 名称。" +
            "多个名称使用 | 分隔，保存顺序即菜单显示顺序。" +
            "留空时使用默认物品和默认排序。"
        );

        _spawnColumns = config.Bind(
            "ItemSpawn",
            "SpawnColumns",
            4,
            new ConfigDescription(
                "物品生成主列表的显示列数。",
                new AcceptableValueRange<int>(
                    1,
                    9
                )
            )
        );
    }

    internal static void SetSpawnColumns(
        int columns)
    {
        if (_spawnColumns is null)
        {
            return;
        }

        _spawnColumns.Value = Math.Max(
            1,
            Math.Min(
                9,
                columns
            )
        );
    }

    internal static IReadOnlyList<string>
        LoadVisibleItemNames()
    {
        string raw =
            _visibleItems?.Value ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(
                new[] { '|' },
                StringSplitOptions.RemoveEmptyEntries
            )
            .Select(value => value.Trim())
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Distinct(
                StringComparer.OrdinalIgnoreCase
            )
            .ToArray();
    }

    internal static void SaveVisibleItemNames(
        IEnumerable<string> names)
    {
        if (_visibleItems is null)
        {
            return;
        }

        string result = string.Join(
            "|",
            names
                .Where(name =>
                    !string.IsNullOrWhiteSpace(name))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
        );

        _visibleItems.Value = result;
    }

    internal static void Clear()
    {
        if (_visibleItems is null)
        {
            return;
        }

        _visibleItems.Value = string.Empty;
    }
}
