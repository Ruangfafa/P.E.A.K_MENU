using System;
using UnityEngine;

namespace P.E.A.K_MENU.Features.BlowDart;

/// <summary>
/// 吹箭效果配置与直接状态应用。
///
/// 不再修改 Action_RaycastDart.afflictionsOnHit。
/// 命中时由 BlowDartPatch 直接调用
/// CharacterAfflictions.AddStatus。
/// </summary>
internal sealed class BlowDartService :
    IDisposable
{
    private const float MinimumAmount =
        0.001f;

    private const float MaximumAmount =
        10f;

    /*
     * 方法 2 使用基础状态值，
     * 不使用 Affliction 的持续时间对象。
     *
     * 保留 Duration 属性只为兼容旧页面或旧配置，
     * 实际命中效果不会读取它。
     */
    private const float MinimumDuration =
        0.1f;

    private const float MaximumDuration =
        600f;

    private bool _enabled;

    private BlowDartEffectType _effectType =
        BlowDartEffectType.Cold;

    private float _amount =
        0.25f;

    private float _duration =
        10f;

    internal bool Enabled =>
        _enabled;

    internal BlowDartEffectType EffectType =>
        _effectType;

    internal float Amount =>
        _amount;

    internal float Duration =>
        _duration;

    internal string LastStatus
    {
        get;
        private set;
    } = "吹箭效果替换尚未开启。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void SetEnabled(
        bool enabled)
    {
        if (_enabled ==
            enabled)
        {
            return;
        }

        _enabled =
            enabled;

        LastSucceeded =
            true;

        LastStatus =
            enabled
                ? $"吹箭效果替换已开启：" +
                  $"{GetEffectDisplayName(_effectType)}。"
                : "吹箭效果替换已关闭，将使用原版睡眠效果。";

        Plugin.Log.LogInfo(
            enabled
                ? "Direct blow dart effect system enabled."
                : "Direct blow dart effect system disabled."
        );
    }

    internal void SetEffectType(
        BlowDartEffectType effectType)
    {
        _effectType =
            effectType;

        LastSucceeded =
            true;

        LastStatus =
            effectType ==
            BlowDartEffectType.Original
                ? "已选择原版睡眠吹箭效果。"
                : $"吹箭效果已设为：" +
                  $"{GetEffectDisplayName(effectType)}。";
    }

    internal void SelectNextEffect()
    {
        BlowDartEffectType[] values =
            (BlowDartEffectType[])
            Enum.GetValues(
                typeof(BlowDartEffectType)
            );

        int currentIndex =
            Array.IndexOf(
                values,
                _effectType
            );

        int nextIndex =
            currentIndex + 1;

        if (nextIndex >=
            values.Length)
        {
            nextIndex =
                0;
        }

        SetEffectType(
            values[nextIndex]
        );
    }

    internal void SelectPreviousEffect()
    {
        BlowDartEffectType[] values =
            (BlowDartEffectType[])
            Enum.GetValues(
                typeof(BlowDartEffectType)
            );

        int currentIndex =
            Array.IndexOf(
                values,
                _effectType
            );

        int previousIndex =
            currentIndex - 1;

        if (previousIndex <
            0)
        {
            previousIndex =
                values.Length - 1;
        }

        SetEffectType(
            values[previousIndex]
        );
    }

    internal void SetAmount(
        float amount)
    {
        if (!IsFiniteNumber(
                amount))
        {
            Fail(
                "吹箭效果强度格式无效。"
            );

            return;
        }

        _amount =
            Mathf.Clamp(
                amount,
                MinimumAmount,
                MaximumAmount
            );

        LastSucceeded =
            true;

        LastStatus =
            $"吹箭效果强度已设为 " +
            $"{_amount:0.###}。";
    }

    internal void SetDuration(
        float duration)
    {
        if (!IsFiniteNumber(
                duration))
        {
            Fail(
                "吹箭持续时间格式无效。"
            );

            return;
        }

        _duration =
            Mathf.Clamp(
                duration,
                MinimumDuration,
                MaximumDuration
            );

        LastSucceeded =
            true;

        LastStatus =
            $"持续时间已保存为 " +
            $"{_duration:0.##} 秒。" +
            $"当前基础状态模式主要使用强度数值。";
    }

    /// <summary>
    /// 直接对命中的角色施加状态。
    ///
    /// 返回 true 表示自定义效果已成功处理，
    /// BlowDartPatch 应阻止原版 DartImpact。
    /// </summary>
    internal bool TryApplyDirectEffect(
    Character target)
{
    if (!_enabled ||
        _effectType ==
        BlowDartEffectType.Original)
    {
        return false;
    }

    if (target is null)
    {
        Fail(
            "吹箭没有命中有效角色。"
        );

        return false;
    }

    if (target.data is null)
    {
        Fail(
            "命中角色的数据不可用。"
        );

        return false;
    }

    if (target.data.dead)
    {
        Fail(
            "命中角色已经死亡，无法施加状态。"
        );

        return false;
    }

    if (target.refs is null ||
        target.refs.afflictions is null ||
        target.refs.view is null)
    {
        Fail(
            "命中角色的网络状态组件不可用。"
        );

        return false;
    }

    if (!TryResolveStatusType(
            _effectType,
            out CharacterAfflictions.STATUSTYPE
                statusType))
    {
        Fail(
            $"效果 {GetEffectDisplayName(_effectType)} " +
            $"不能通过基础状态同步。"
        );

        return false;
    }

    try
    {
        int statusIndex =
            (int)statusType;

        int statusCount =
            CharacterAfflictions
                .NumStatusTypes;

        if (statusIndex < 0 ||
            statusIndex >= statusCount)
        {
            Fail(
                $"状态编号无效：" +
                $"{statusIndex}。"
            );

            return false;
        }

        /*
         * 这是状态增量数组，不是最终状态数组。
         *
         * 其他位置全部为 0，
         * 只有选中的状态增加 _amount。
         */
        float[] statusChanges =
            new float[
                statusCount
            ];

        statusChanges[
            statusIndex
        ] =
            _amount;

        /*
         * 使用 PEAK 自带的网络状态 RPC。
         *
         * 它会在目标 Character 的网络对象上，
         * 将状态变化同步到对应玩家。
         */
        target
            .refs
            .view
            .RPC(
                "RPC_ApplyStatusesFromFloatArray",
                Photon.Pun.RpcTarget.All,
                new object[]
                {
                    statusChanges
                }
            );

        LastSucceeded =
            true;

        LastStatus =
            $"已向 {target.characterName} " +
            $"发送{GetEffectDisplayName(_effectType)}效果，" +
            $"强度 {_amount:0.###}。";

        Plugin.Log.LogInfo(
            $"Blow dart status RPC sent: " +
            $"target={target.characterName}; " +
            $"effect={_effectType}; " +
            $"statusIndex={statusIndex}; " +
            $"amount={_amount:0.###}."
        );

        return true;
    }
    catch (Exception exception)
    {
        Fail(
            $"发送吹箭状态 RPC 失败：" +
            $"{exception.GetBaseException().Message}"
        );

        Plugin.Log.LogError(
            $"Blow dart status RPC failed: " +
            $"{exception}"
        );

        return false;
    }
}

    public void Dispose()
    {
        _enabled =
            false;
    }

    internal static string GetEffectDisplayName(
        BlowDartEffectType effectType)
    {
        return effectType switch
        {
            BlowDartEffectType.Original =>
                "原版睡眠",

            BlowDartEffectType.Injury =>
                "受伤",

            BlowDartEffectType.Poison =>
                "中毒",

            BlowDartEffectType.Cold =>
                "寒冷",

            BlowDartEffectType.Hot =>
                "炎热",

            BlowDartEffectType.Hunger =>
                "饥饿",

            BlowDartEffectType.Drowsy =>
                "困倦",

            BlowDartEffectType.Curse =>
                "诅咒",

            _ =>
                effectType.ToString()
        };
    }

    private static bool TryResolveStatusType(
        BlowDartEffectType effectType,
        out CharacterAfflictions.STATUSTYPE statusType)
    {
        switch (effectType)
        {
            case BlowDartEffectType.Injury:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)0;

                return true;

            case BlowDartEffectType.Hunger:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)1;

                return true;

            case BlowDartEffectType.Cold:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)2;

                return true;

            case BlowDartEffectType.Poison:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)3;

                return true;

            case BlowDartEffectType.Curse:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)5;

                return true;

            case BlowDartEffectType.Drowsy:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)6;

                return true;

            case BlowDartEffectType.Hot:
                statusType =
                    (CharacterAfflictions.STATUSTYPE)8;

                return true;

            default:
                statusType =
                    default;

                return false;
        }
    }

    private static bool IsFiniteNumber(
        float value)
    {
        return
            !float.IsNaN(
                value) &&
            !float.IsInfinity(
                value);
    }

    private void Fail(
        string message)
    {
        LastSucceeded =
            false;

        LastStatus =
            message;

        Plugin.Log.LogWarning(
            message
        );
    }
    
    internal void GiveBlowDart()
{
    Character? character =
        Character.localCharacter;

    if (character is null ||
        !character.IsLocal)
    {
        Fail(
            "未找到本地玩家，请先进入关卡。"
        );

        return;
    }

    if (character.data is null ||
        character.data.dead ||
        character.IsGhost)
    {
        Fail(
            "当前角色无法持有吹箭。"
        );

        return;
    }

    if (GameUtils.instance is null)
    {
        Fail(
            "GameUtils 尚未初始化，无法生成吹箭。"
        );

        return;
    }

    Item? blowDartItem =
        FindBlowDartItem();

    if (blowDartItem is null)
    {
        Fail(
            "没有在游戏资源中找到吹箭物品。"
        );

        return;
    }

    try
    {
        /*
         * 使用 PEAK 自带的物品生成和拾取流程。
         *
         * 背包或手部有空位时会尝试直接给予玩家；
         * 无法直接持有时，游戏会按自身逻辑处理。
         */
        GameUtils
            .instance
            .InstantiateAndGrab(
                blowDartItem,
                character,
                0
            );

        LastSucceeded =
            true;

        LastStatus =
            "已获取一支吹箭。";

        Plugin.Log.LogInfo(
            $"Blow dart spawned: " +
            $"{blowDartItem.name}."
        );
    }
    catch (Exception exception)
    {
        Fail(
            $"获取吹箭失败：" +
            $"{exception.GetBaseException().Message}"
        );

        Plugin.Log.LogError(
            $"Failed to spawn blow dart: " +
            $"{exception}"
        );
    }
}

private static Item? FindBlowDartItem()
{
    Item[] items;

    try
    {
        items =
            Resources
                .FindObjectsOfTypeAll<Item>();
    }
    catch (Exception exception)
    {
        Plugin.Log.LogError(
            $"Failed to search item resources: " +
            $"{exception}"
        );

        return null;
    }

    /*
     * 最可靠的判断方式：
     * 检查物品是否包含 Action_RaycastDart。
     */
    foreach (Item item
             in items)
    {
        if (item is null ||
            item.gameObject is null)
        {
            continue;
        }

        Action_RaycastDart? dartAction =
            item.GetComponent<
                Action_RaycastDart>();

        if (dartAction is null)
        {
            dartAction =
                item.GetComponentInChildren<
                    Action_RaycastDart>(
                        true
                    );
        }

        if (dartAction is null)
        {
            continue;
        }

        Plugin.Log.LogInfo(
            $"Resolved blow dart item by " +
            $"Action_RaycastDart: {item.name}."
        );

        return item;
    }

    /*
     * 组件搜索失败时再使用名称兜底。
     */
    foreach (Item item
             in items)
    {
        if (item is null)
        {
            continue;
        }

        string itemName =
            item.name ??
            string.Empty;

        string displayName;

        try
        {
            displayName =
                item.GetName() ??
                string.Empty;
        }
        catch
        {
            displayName =
                string.Empty;
        }

        if (ContainsBlowDartName(
                itemName) ||
            ContainsBlowDartName(
                displayName))
        {
            Plugin.Log.LogInfo(
                $"Resolved blow dart item by name: " +
                $"{item.name}."
            );

            return item;
        }
    }

    return null;
}

private static bool ContainsBlowDartName(
    string value)
{
    if (string.IsNullOrWhiteSpace(
            value))
    {
        return false;
    }

    return
        value.Contains(
            "Blowgun",
            StringComparison.OrdinalIgnoreCase
        ) ||
        value.Contains(
            "Blowdart",
            StringComparison.OrdinalIgnoreCase
        ) ||
        value.Contains(
            "Blow Dart",
            StringComparison.OrdinalIgnoreCase
        ) ||
        value.Contains(
            "Dart",
            StringComparison.OrdinalIgnoreCase
        ) ||
        value.Contains(
            "吹箭",
            StringComparison.OrdinalIgnoreCase
        );
}
}