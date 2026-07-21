using UnityEngine;

namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// 表示从 PEAK 物品数据库中发现的一项物品。
/// </summary>
internal sealed class ItemSpawnEntry
{
    internal ItemSpawnEntry(
        Item prefab,
        string displayName,
        Sprite? icon)
    {
        Prefab = prefab;

        PrefabName =
            prefab.name ?? string.Empty;

        DisplayName =
            string.IsNullOrWhiteSpace(displayName)
                ? PrefabName
                : displayName;

        Icon = icon;
    }

    /// <summary>
    /// PEAK 的物品预制体。
    /// </summary>
    internal Item Prefab { get; }

    /// <summary>
    /// 用于保存配置和查找物品的名称。
    /// </summary>
    internal string PrefabName { get; }

    /// <summary>
    /// 菜单中显示的名称。
    /// </summary>
    internal string DisplayName { get; }

    /// <summary>
    /// 从游戏物品数据中取得的图标。
    /// 读取失败时允许为空。
    /// </summary>
    internal Sprite? Icon { get; }

    public override string ToString()
    {
        return $"{DisplayName} ({PrefabName})";
    }
}