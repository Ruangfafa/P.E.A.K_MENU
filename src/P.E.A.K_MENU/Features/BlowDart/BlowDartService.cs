using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using P.E.A.K_MENU.Features.ItemSpawn;
using Photon.Pun;
using UnityEngine;

namespace P.E.A.K_MENU.Features.BlowDart;

internal sealed class BlowDartService :
    IDisposable
{
    private const int MinimumAmountPercent = 1;
    private const int MaximumAmountPercent = 200;
    private const float MinimumDuration = 0.1f;
    private const float MaximumDuration = 600f;

    private readonly ConfigEntry<BlowDartEffectType>
        _effectType;

    private readonly ConfigEntry<int>
        _amountPercent;

    private readonly ConfigEntry<float>
        _durationSeconds;

    private readonly Dictionary<int, float>
        _whirlwindStopTimes = new();

    private Item? _blowDartItem;
    private Texture2D? _blowDartIcon;

    internal BlowDartEffectType EffectType =>
        _effectType.Value;

    internal int AmountPercent =>
        Mathf.Clamp(
            _amountPercent.Value,
            MinimumAmountPercent,
            MaximumAmountPercent
        );

    internal float DurationSeconds =>
        Mathf.Clamp(
            _durationSeconds.Value,
            MinimumDuration,
            MaximumDuration
        );

    internal Texture2D? Icon
    {
        get
        {
            ResolveBlowDartItem();
            return _blowDartIcon;
        }
    }

    internal string LastStatus
    {
        get;
        private set;
    } = "请选择吹箭效果。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal BlowDartService(
        ConfigFile config)
    {
        _effectType = config.Bind(
            "BlowDart",
            "Effect",
            BlowDartEffectType.None,
            "本地玩家发射吹箭时赋予目标的效果。"
        );

        _amountPercent = config.Bind(
            "BlowDart",
            "AmountPercent",
            20,
            new ConfigDescription(
                "数值状态的百分制增量；100 对应底层 1。",
                new AcceptableValueRange<int>(
                    MinimumAmountPercent,
                    MaximumAmountPercent
                )
            )
        );

        _durationSeconds = config.Bind(
            "BlowDart",
            "DurationSeconds",
            5f,
            new ConfigDescription(
                "小旋风效果持续秒数。",
                new AcceptableValueRange<float>(
                    MinimumDuration,
                    MaximumDuration
                )
            )
        );
    }

    internal void SetEffectType(
        BlowDartEffectType effectType)
    {
        _effectType.Value = effectType;

        LastSucceeded = true;
        LastStatus =
            $"吹箭效果已设为：" +
            $"{GetDisplayName(effectType)}。";
    }

    internal void SetAmountPercent(
        int amountPercent)
    {
        _amountPercent.Value = Mathf.Clamp(
            amountPercent,
            MinimumAmountPercent,
            MaximumAmountPercent
        );

        LastSucceeded = true;
        LastStatus =
            $"吹箭赋予值已设为 " +
            $"{AmountPercent}%。";
    }

    internal void SetDurationSeconds(
        float durationSeconds)
    {
        if (float.IsNaN(durationSeconds) ||
            float.IsInfinity(durationSeconds))
        {
            return;
        }

        _durationSeconds.Value = Mathf.Clamp(
            durationSeconds,
            MinimumDuration,
            MaximumDuration
        );

        LastSucceeded = true;
        LastStatus =
            $"小旋风时间已设为 " +
            $"{DurationSeconds:0.##} 秒。";
    }

    internal bool TryHandleHit(
        Action_RaycastDart action,
        Character target,
        Vector3 origin,
        Vector3 endpoint)
    {
        if (action is null ||
            target is null)
        {
            return false;
        }

        try
        {
            SendVisualOnlyImpact(
                action,
                origin,
                endpoint
            );

            if (EffectType ==
                BlowDartEffectType.Whirlwind)
            {
                ApplyWhirlwind(target);
                return true;
            }

            if (!TryResolveStatusType(
                    EffectType,
                    out CharacterAfflictions.STATUSTYPE
                        statusType))
            {
                return false;
            }

            ApplyStatus(
                target,
                statusType,
                AmountPercent / 100f
            );

            LastSucceeded = true;
            LastStatus =
                $"已向 {target.characterName} 赋予" +
                $"{GetDisplayName(EffectType)} " +
                $"{AmountPercent}%。";

            return true;
        }
        catch (Exception exception)
        {
            LastSucceeded = false;
            LastStatus =
                $"吹箭效果应用失败：" +
                $"{exception.GetBaseException().Message}";

            Plugin.Log.LogError(
                $"Blow dart effect failed: {exception}"
            );

            return false;
        }
    }

    internal void GiveBlowDart()
    {
        Character? character =
            Character.localCharacter;

        Item? item = ResolveBlowDartItem();

        if (character is null ||
            !character.IsLocal)
        {
            Fail("未找到本地玩家，请先进入关卡。");
            return;
        }

        if (item is null)
        {
            Fail("没有在游戏物品数据库中找到吹箭。");
            return;
        }

        if (GameUtils.instance is null)
        {
            Fail("GameUtils 尚未初始化，无法生成吹箭。");
            return;
        }

        try
        {
            GameUtils.instance.InstantiateAndGrab(
                item,
                character,
                0
            );

            LastSucceeded = true;
            LastStatus = "已获取吹箭。";
        }
        catch (Exception exception)
        {
            Fail(
                $"获取吹箭失败：" +
                $"{exception.GetBaseException().Message}"
            );
        }
    }

    internal void Update()
    {
        if (_whirlwindStopTimes.Count == 0)
        {
            return;
        }

        float now = Time.unscaledTime;
        var completed = new List<int>();

        foreach (KeyValuePair<int, float> entry
                 in _whirlwindStopTimes)
        {
            if (now < entry.Value)
            {
                continue;
            }

            SendWhirlwindStop(entry.Key);
            completed.Add(entry.Key);
        }

        foreach (int viewId in completed)
        {
            _whirlwindStopTimes.Remove(viewId);
        }
    }

    public void Dispose()
    {
        foreach (int viewId
                 in new List<int>(
                     _whirlwindStopTimes.Keys
                 ))
        {
            SendWhirlwindStop(viewId);
        }

        _whirlwindStopTimes.Clear();
        _blowDartItem = null;
        _blowDartIcon = null;
    }

    internal static bool UsesAmount(
        BlowDartEffectType effectType)
    {
        return effectType != BlowDartEffectType.None &&
               effectType != BlowDartEffectType.Whirlwind;
    }

    internal static string GetDisplayName(
        BlowDartEffectType effectType)
    {
        return effectType switch
        {
            BlowDartEffectType.None => "无效果（原版）",
            BlowDartEffectType.Injury => "受伤",
            BlowDartEffectType.Hunger => "饥饿",
            BlowDartEffectType.Poison => "中毒",
            BlowDartEffectType.Cold => "寒冷",
            BlowDartEffectType.Hot => "炎热",
            BlowDartEffectType.Drowsy => "困倦",
            BlowDartEffectType.Curse => "诅咒",
            BlowDartEffectType.Spores => "孢子",
            BlowDartEffectType.Petrify => "石化",
            BlowDartEffectType.Whirlwind => "小旋风",
            _ => effectType.ToString()
        };
    }

    private static void SendVisualOnlyImpact(
        Action_RaycastDart action,
        Vector3 origin,
        Vector3 endpoint)
    {
        PhotonView? view =
            action.GetComponentInParent<PhotonView>();

        if (view is null)
        {
            throw new InvalidOperationException(
                "吹箭 PhotonView 不可用。"
            );
        }

        view.RPC(
            "RPC_DartImpact",
            RpcTarget.All,
            -1,
            origin,
            endpoint
        );
    }

    private static void ApplyStatus(
        Character target,
        CharacterAfflictions.STATUSTYPE statusType,
        float amount)
    {
        PhotonView view = target.photonView;

        if (PhotonNetwork.IsMasterClient)
        {
            float[] changes =
                new float[
                    CharacterAfflictions.NumStatusTypes
                ];

            changes[(int)statusType] = amount;

            view.RPC(
                "RPC_ApplyStatusesFromFloatArray",
                RpcTarget.All,
                new object[]
                {
                    changes
                }
            );

            return;
        }

        Rigidbody? hip =
            target.GetBodypartRig(
                BodypartType.Hip
            );

        Vector3 position =
            hip is null
                ? target.Center
                : hip.transform.position;

        /*
         * 非房主没有通用状态写入权限。
         * RPCA_Stick 是游戏原生、所有客户端都具备的状态载体。
         * Hip 不会被原版仙人掌用于固定；紧接着只清理 Hip
         * 的临时关节，不会解除玩家真实的四肢固定状态。
         */
        view.RPC(
            "RPCA_Stick",
            RpcTarget.All,
            BodypartType.Hip,
            position,
            position,
            statusType,
            amount
        );

        view.RPC(
            "RPCA_ClearJoint",
            RpcTarget.All,
            BodypartType.Hip
        );
    }

    private void ApplyWhirlwind(
        Character target)
    {
        int viewId = target.photonView.ViewID;

        target.photonView.RPC(
            "StartWhirlwindRPC",
            RpcTarget.All
        );

        _whirlwindStopTimes[viewId] =
            Time.unscaledTime +
            DurationSeconds;

        LastSucceeded = true;
        LastStatus =
            $"已向 {target.characterName} 赋予小旋风 " +
            $"{DurationSeconds:0.##} 秒。";
    }

    private static void SendWhirlwindStop(
        int viewId)
    {
        try
        {
            PhotonView? view =
                PhotonView.Find(viewId);

            if (view is not null)
            {
                view.RPC(
                    "StopWhirlwindRPC",
                    RpcTarget.All
                );
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.LogDebug(
                $"Failed to stop blow dart whirlwind: " +
                $"{exception.Message}"
            );
        }
    }

    private Item? ResolveBlowDartItem()
    {
        if (_blowDartItem is not null)
        {
            return _blowDartItem;
        }

        if (!ItemSpawnRuntime.IsInitialized)
        {
            return null;
        }

        ItemSpawnRuntime.Catalog.RefreshIfNeeded();

        foreach (ItemSpawnEntry entry
                 in ItemSpawnRuntime.Catalog.AllItems)
        {
            Item item = entry.Prefab;

            if (item.GetComponentInChildren<
                    Action_RaycastDart>(true) is null)
            {
                continue;
            }

            _blowDartItem = item;
            _blowDartIcon = entry.Icon?.texture;

            Plugin.Log.LogInfo(
                $"Resolved blow dart item: {item.name}."
            );

            return item;
        }

        return null;
    }

    private static bool TryResolveStatusType(
        BlowDartEffectType effectType,
        out CharacterAfflictions.STATUSTYPE statusType)
    {
        statusType = effectType switch
        {
            BlowDartEffectType.Injury =>
                CharacterAfflictions.STATUSTYPE.Injury,
            BlowDartEffectType.Hunger =>
                CharacterAfflictions.STATUSTYPE.Hunger,
            BlowDartEffectType.Poison =>
                CharacterAfflictions.STATUSTYPE.Poison,
            BlowDartEffectType.Cold =>
                CharacterAfflictions.STATUSTYPE.Cold,
            BlowDartEffectType.Hot =>
                CharacterAfflictions.STATUSTYPE.Hot,
            BlowDartEffectType.Drowsy =>
                CharacterAfflictions.STATUSTYPE.Drowsy,
            BlowDartEffectType.Curse =>
                CharacterAfflictions.STATUSTYPE.Curse,
            BlowDartEffectType.Spores =>
                CharacterAfflictions.STATUSTYPE.Spores,
            BlowDartEffectType.Petrify =>
                CharacterAfflictions.STATUSTYPE.Petrify,
            _ => default
        };

        return UsesAmount(effectType);
    }

    private void Fail(
        string message)
    {
        LastSucceeded = false;
        LastStatus = message;
        Plugin.Log.LogWarning(message);
    }
}
