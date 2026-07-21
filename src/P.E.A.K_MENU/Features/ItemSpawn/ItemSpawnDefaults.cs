namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// ItemSpawn 默认显示的物品以及默认排序。
///
/// 这里填写的是候选名称。
/// 游戏运行后会在实际物品数据库中进行匹配。
/// 当前版本不存在的物品会自动跳过。
/// </summary>
internal static class ItemSpawnDefaults
{
    internal static readonly string[] PreferredPrefabNames =
    {
        "ScoutEffigy",
        "Backpack",
        "BookOfBones",
        "Airplane Food",
        "Cure-All",
        "RopeSpool",
        "Anti-Rope Spool",
        "RopeShooter",
        "RopeShooterAnti",
        "RescueHook",
        "RescueHook_Infinite",
        "PortableStovetopItem",
        "HealingDart Variant",
    };

    /// <summary>
    /// 如果默认候选一个都没有匹配到，
    /// 则使用物品数据库排序后的前几个物品。
    /// </summary>
    internal const int FallbackItemCount = 12;
}