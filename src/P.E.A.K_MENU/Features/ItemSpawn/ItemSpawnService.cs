using System;

namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// 负责调用 PEAK 自身的物品网络生成流程。
/// </summary>
internal sealed class ItemSpawnService
{
    internal string LastStatus
    {
        get;
        private set;
    } = "选择物品以生成。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void Spawn(
        ItemSpawnEntry entry)
    {
        LastSucceeded = false;

        if (entry.Prefab is null)
        {
            LastStatus =
                "物品预制体无效。";

            return;
        }

        Character player =
            Character.localCharacter;

        if (player is null)
        {
            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (GameUtils.instance is null)
        {
            LastStatus =
                "GameUtils 尚未初始化，请稍后再试。";

            return;
        }

        try
        {
            /*
             * 使用 PEAK 自身的网络生成和抓取流程。
             *
             * 参数说明：
             * entry.Prefab：
             *     需要生成的物品预制体。
             *
             * player：
             *     接收物品的本地角色。
             *
             * 0：
             *     优先放入的物品槽位索引。
             *
             * 游戏会负责网络物品创建以及抓取流程。
             * 当角色不能正常抓取时，后续需要根据
             * 实际游戏表现继续补充明确的掉落逻辑。
             */
            GameUtils.instance
                .InstantiateAndGrab(
                    entry.Prefab,
                    player,
                    0
                );

            LastSucceeded = true;

            LastStatus =
                $"已生成：{entry.DisplayName}";

            Plugin.Log.LogInfo(
                $"Spawned item " +
                $"'{entry.PrefabName}' through " +
                $"InstantiateAndGrab."
            );
        }
        catch (Exception exception)
        {
            LastSucceeded = false;

            LastStatus =
                $"生成失败：{exception.Message}";

            Plugin.Log.LogError(
                $"Failed to spawn " +
                $"'{entry.PrefabName}': " +
                $"{exception}"
            );
        }
    }

    internal void ClearStatus()
    {
        LastSucceeded = false;
        LastStatus = "选择物品以生成。";
    }
}