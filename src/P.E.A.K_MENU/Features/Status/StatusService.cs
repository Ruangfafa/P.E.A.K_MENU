using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using P.E.A.K_MENU.Patches;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Status;

/// <summary>
/// 本地玩家状态管理。
/// </summary>
internal sealed class StatusService :
    IDisposable
{
    private const float MaintenanceInterval =
        0.05f;

    private static readonly PropertyInfo?
        InfiniteStaminaProperty =
            typeof(Character).GetProperty(
                "infiniteStam",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

    private static readonly FieldInfo?
        InfiniteStaminaField =
            typeof(Character).GetField(
                "infiniteStam",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

    private static readonly PropertyInfo?
        StatusesLockedProperty =
            typeof(Character).GetProperty(
                "statusesLocked",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

    private static readonly FieldInfo?
        StatusesLockedField =
            typeof(Character).GetField(
                "statusesLocked",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

    private static readonly MethodInfo?
        GetStatusMethod =
            FindGetStatusMethod();

    private readonly Dictionary<
        CharacterAfflictions.STATUSTYPE,
        ActiveTimedStatus>
        _timedStatuses = new();

    private readonly List<StatusEffectDefinition>
        _specialEffects = new();

    private float _nextMaintenanceTime;

    private bool _invincible;
    private bool _antiKnockback = true;
    private bool _infiniteStamina;
    
    private bool _flightProtectionLock;

    private bool _weightOverrideEnabled;
    private float _customWeight;

    private Character? _lastCharacter;

    internal StatusService()
    {
        BuildStatusEffectList();
        LogRuntimeMembers();
    }

    internal bool Invincible =>
        _invincible;

    internal bool AntiKnockback =>
        _antiKnockback;

    internal bool InfiniteStamina =>
        _infiniteStamina;
    
    internal bool FlightProtectionLock =>
        _flightProtectionLock;

    internal bool WeightOverrideEnabled =>
        _weightOverrideEnabled;

    internal float CustomWeight =>
        _customWeight;

    internal IReadOnlyList<StatusEffectDefinition>
        SpecialEffects =>
            _specialEffects;

    internal string LastStatus
    {
        get;
        private set;
    } = "等待操作。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void Update()
    {
        if (Time.unscaledTime <
            _nextMaintenanceTime)
        {
            return;
        }

        _nextMaintenanceTime =
            Time.unscaledTime +
            MaintenanceInterval;

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            _lastCharacter = null;
            return;
        }

        if (!ReferenceEquals(
                _lastCharacter,
                character))
        {
            _lastCharacter = character;

            ApplyPersistentSettings(
                character
            );
        }

        if (_infiniteStamina)
        {
            SetCharacterBoolean(
                character,
                InfiniteStaminaProperty,
                InfiniteStaminaField,
                true
            );
        }

        if (_invincible)
        {
            MaintainInvincibility(
                character
            );
        }

        MaintainTimedStatuses(
            character
        );

        if (_weightOverrideEnabled)
        {
            ApplyWeight(
                character,
                _customWeight
            );
        }
    }

    internal void SetInvincible(
        bool enabled,
        bool force = false)
    {
        if (_flightProtectionLock &&
            !force &&
            !enabled)
        {
            LastSucceeded = false;
            LastStatus =
                "飞行总开关开启期间，无敌已被锁定。";

            return;
        }

        _invincible = enabled;

        StatusProtectionPatch
                .InvincibleEnabled =
            enabled;

        Character? character =
            Character.localCharacter;

        if (!enabled)
        {
            if (character is not null)
            {
                SetCharacterBoolean(
                    character,
                    StatusesLockedProperty,
                    StatusesLockedField,
                    false
                );
            }

            LastSucceeded = true;
            LastStatus = "无敌已关闭。";
            return;
        }

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        bool locked =
            SetCharacterBoolean(
                character,
                StatusesLockedProperty,
                StatusesLockedField,
                true
            );

        ClearNegativeEffectsInternal(
            character,
            preserveWeight: true,
            preserveTimedStatuses: true,
            showResult: false
        );

        LastSucceeded = true;

        if (_antiKnockback)
        {
            LastStatus =
                locked
                    ? "无敌已开启：锁定状态，并阻止死亡、击退与摔倒。"
                    : "无敌已开启：阻止死亡、击退与摔倒。";
        }
        else
        {
            LastStatus =
                locked
                    ? "无敌已开启：锁定状态并阻止死亡。"
                    : "无敌已开启：阻止死亡。";
        }
    }

    internal void SetAntiKnockback(
        bool enabled,
        bool force = false)
    {
        if (_flightProtectionLock &&
            !force &&
            !enabled)
        {
            LastSucceeded = false;
            LastStatus =
                "飞行总开关开启期间，防击退已被锁定。";

            return;
        }

        _antiKnockback = enabled;

        StatusProtectionPatch
                .AntiKnockbackEnabled =
            enabled;

        LastSucceeded = true;

        LastStatus =
            enabled
                ? "附加保护已开启：阻止击退、摔倒与外力。"
                : "附加保护已关闭：恢复原版击退与摔倒。";
    }
    
    internal void SetFlightProtectionLock(
        bool locked)
    {
        _flightProtectionLock =
            locked;

        if (locked)
        {
            LastSucceeded = true;
            LastStatus =
                "飞行功能已锁定无敌与防击退。";
        }
        else
        {
            LastSucceeded = true;
            LastStatus =
                "飞行保护锁定已解除。";
        }
    }

    internal void SetInfiniteStamina(
        bool enabled)
    {
        _infiniteStamina = enabled;

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        bool changed =
            SetCharacterBoolean(
                character,
                InfiniteStaminaProperty,
                InfiniteStaminaField,
                enabled
            );

        LastSucceeded = changed;

        LastStatus =
            changed
                ? enabled
                    ? "无限体力已开启。"
                    : "无限体力已关闭。"
                : "没有找到 infiniteStam 成员。";

        Plugin.Log.LogInfo(
            $"SetInfiniteStamina({enabled}): " +
            $"changed={changed}."
        );
    }

    internal void ClearNegativeEffects()
    {
        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        ClearNegativeEffectsInternal(
            character,
            preserveWeight: true,
            preserveTimedStatuses: false,
            showResult: true
        );
    }

    internal void SetWeightOverride(
        bool enabled)
    {
        _weightOverrideEnabled =
            enabled;

        StatusProtectionPatch
            .WeightOverrideEnabled =
                enabled;

        if (!enabled)
        {
            LastSucceeded = true;

            LastStatus =
                "负重覆盖已关闭，游戏将恢复计算真实负重。";

            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        bool applied =
            ApplyWeight(
                character,
                _customWeight
            );

        LastSucceeded = applied;

        LastStatus =
            applied
                ? $"负重覆盖已开启：{_customWeight:0.##}。"
                : "没有找到 CharacterAfflictions。";
    }

    internal void SetWeight(
        float weight)
    {
        if (!IsFiniteNumber(weight))
        {
            Fail("负重数值无效。");
            return;
        }

        weight = Mathf.Clamp(
            weight,
            0f,
            1000f
        );

        _customWeight = weight;
        _weightOverrideEnabled = true;

        StatusProtectionPatch
            .WeightOverrideEnabled =
                true;

        StatusProtectionPatch
            .WeightOverrideValue =
                weight;

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        bool applied =
            ApplyWeight(
                character,
                weight
            );

        LastSucceeded = applied;

        LastStatus =
            applied
                ? $"负重已覆盖为 {weight:0.##}。"
                : "没有找到 CharacterAfflictions。";
    }

    internal void ApplySpecialEffect(
        StatusEffectDefinition effect,
        float amount,
        float duration,
        StatusApplyMode applyMode)
    {
        if (effect is null)
        {
            Fail("特殊效果为空。");
            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            Fail(
                "未找到本地玩家，请先进入关卡。"
            );

            return;
        }

        if (effect.ShowAmount &&
            !IsFiniteNumber(amount))
        {
            Fail("持有量数值无效。");
            return;
        }

        if (effect.ShowDuration &&
            (!IsFiniteNumber(duration) ||
             duration <= 0f))
        {
            Fail("持续时间必须大于 0 秒。");
            return;
        }

        amount = Mathf.Clamp(
            amount,
            -10000f,
            10000f
        );

        duration = Mathf.Clamp(
            duration,
            0.1f,
            36000f
        );

        bool applied;

        switch (effect.Kind)
        {
            case StatusEffectKind.GameStatus:
                applied =
                    ApplyGameStatus(
                        character,
                        effect,
                        amount,
                        duration,
                        applyMode
                    );
                break;

            default:
                applied = false;
                break;
        }

        LastSucceeded = applied;

        if (!applied)
        {
            LastStatus =
                $"无法施加“{effect.DisplayName}”。";

            return;
        }

        LastStatus =
            BuildAppliedStatusMessage(
                effect,
                amount,
                duration,
                applyMode
            );

        Plugin.Log.LogInfo(
            $"ApplySpecialEffect: " +
            $"id={effect.Id}, " +
            $"amount={amount}, " +
            $"duration={duration}, " +
            $"mode={applyMode}, " +
            $"valueMode={effect.ValueMode}."
        );
    }

    internal bool TryReadStatus(
        StatusEffectDefinition effect,
        out float value)
    {
        value = 0f;

        if (!effect.StatusType.HasValue)
        {
            return false;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            return false;
        }

        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            return false;
        }

        return TryGetStatusValue(
            afflictions,
            effect.StatusType.Value,
            out value
        );
    }

    internal void SetInputError(
        string message)
    {
        Fail(message);
    }

    public void Dispose()
    {
        Character? character =
            Character.localCharacter;

        if (character is not null)
        {
            SetCharacterBoolean(
                character,
                InfiniteStaminaProperty,
                InfiniteStaminaField,
                false
            );

            SetCharacterBoolean(
                character,
                StatusesLockedProperty,
                StatusesLockedField,
                false
            );

            ClearTimedStatuses(
                character
            );
        }

        _invincible = false;
        _antiKnockback = true;
        _infiniteStamina = false;
        
        _flightProtectionLock = false;

        _weightOverrideEnabled = false;
        _customWeight = 0f;

        StatusProtectionPatch
            .InvincibleEnabled =
                false;

        StatusProtectionPatch
            .AntiKnockbackEnabled =
                true;

        StatusProtectionPatch
            .WeightOverrideEnabled =
                false;

        StatusProtectionPatch
            .WeightOverrideValue =
                0f;

        _timedStatuses.Clear();
        _lastCharacter = null;
    }

    private void ApplyPersistentSettings(
        Character character)
    {
        SetCharacterBoolean(
            character,
            InfiniteStaminaProperty,
            InfiniteStaminaField,
            _infiniteStamina
        );

        SetCharacterBoolean(
            character,
            StatusesLockedProperty,
            StatusesLockedField,
            _invincible
        );

        if (_weightOverrideEnabled)
        {
            ApplyWeight(
                character,
                _customWeight
            );
        }
    }

    private void MaintainInvincibility(
        Character character)
    {
        SetCharacterBoolean(
            character,
            StatusesLockedProperty,
            StatusesLockedField,
            true
        );

        /*
         * 无敌只持续清除可能致命的普通状态，
         * 不清除用户通过状态合集主动添加的计时状态。
         */
        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            return;
        }

        foreach (
            CharacterAfflictions.STATUSTYPE
                statusType
            in Enum.GetValues(
                typeof(
                    CharacterAfflictions
                        .STATUSTYPE)))
        {
            string name =
                statusType.ToString()
                    .ToLowerInvariant();

            bool dangerous =
                name.Contains("injury") ||
                name.Contains("poison") ||
                name.Contains("curse");

            if (!dangerous)
            {
                continue;
            }

            if (_timedStatuses.ContainsKey(
                    statusType))
            {
                continue;
            }

            TrySetStatusValue(
                afflictions,
                statusType,
                0f
            );
        }
    }

    private void ClearNegativeEffectsInternal(
        Character character,
        bool preserveWeight,
        bool preserveTimedStatuses,
        bool showResult)
    {
        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            if (showResult)
            {
                Fail(
                    "没有找到 CharacterAfflictions。"
                );
            }

            return;
        }

        int cleared = 0;

        foreach (
            CharacterAfflictions.STATUSTYPE
                statusType
            in Enum.GetValues(
                typeof(
                    CharacterAfflictions
                        .STATUSTYPE)))
        {
            string statusName =
                statusType.ToString();

            if (preserveWeight &&
                statusName.Equals(
                    "Weight",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                continue;
            }

            if (preserveTimedStatuses &&
                _timedStatuses.ContainsKey(
                    statusType))
            {
                continue;
            }

            if (TrySetStatusValue(
                    afflictions,
                    statusType,
                    0f))
            {
                cleared++;
            }
        }

        if (!preserveTimedStatuses)
        {
            _timedStatuses.Clear();
        }

        if (!showResult)
        {
            return;
        }

        LastSucceeded = cleared > 0;

        LastStatus =
            cleared > 0
                ? $"已清除 {cleared} 项角色状态。"
                : "没有清除任何状态。";
    }

    private bool ApplyWeight(
        Character character,
        float weight)
    {
        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            return false;
        }

        bool result =
            TrySetStatusValue(
                afflictions,
                CharacterAfflictions
                    .STATUSTYPE
                    .Weight,
                weight
            );

        if (result)
        {
            StatusProtectionPatch
                .WeightOverrideValue =
                    weight;
        }

        return result;
    }

    private bool ApplyGameStatus(
        Character character,
        StatusEffectDefinition effect,
        float inputAmount,
        float duration,
        StatusApplyMode applyMode)
    {
        if (!effect.StatusType.HasValue)
        {
            return false;
        }

        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            return false;
        }

        CharacterAfflictions.STATUSTYPE
            statusType =
                effect.StatusType.Value;

        float currentAmount = 0f;

        TryGetStatusValue(
            afflictions,
            statusType,
            out currentAmount
        );

        float finalAmount =
            CalculateFinalAmount(
                currentAmount,
                inputAmount,
                applyMode
            );

        finalAmount = Mathf.Clamp(
            finalAmount,
            0f,
            10000f
        );

        switch (effect.ValueMode)
        {
            case StatusValueMode.AmountOnly:
            case StatusValueMode.NativeDecay:
                /*
                 * 只写入一次。
                 *
                 * NativeDecay 不进入菜单计时器，
                 * 之后完全交给游戏自身更新。
                 */
                _timedStatuses.Remove(
                    statusType
                );

                return TrySetStatusValue(
                    afflictions,
                    statusType,
                    finalAmount
                );

            case StatusValueMode.TimedConstant:
            case StatusValueMode.TimedDecay:
                if (!TrySetStatusValue(
                        afflictions,
                        statusType,
                        finalAmount))
                {
                    return false;
                }

                if (finalAmount <= 0f ||
                    applyMode ==
                    StatusApplyMode.Clear)
                {
                    _timedStatuses.Remove(
                        statusType
                    );

                    return true;
                }

                _timedStatuses[
                    statusType] =
                        new ActiveTimedStatus(
                            statusType,
                            effect.ValueMode,
                            finalAmount,
                            Time.unscaledTime,
                            Time.unscaledTime +
                            duration
                        );

                return true;

            default:
                return false;
        }
    }

    private void MaintainTimedStatuses(
        Character character)
    {
        if (_timedStatuses.Count == 0)
        {
            return;
        }

        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is null)
        {
            return;
        }

        float now =
            Time.unscaledTime;

        CharacterAfflictions.STATUSTYPE[]
            statusTypes =
                _timedStatuses
                    .Keys
                    .ToArray();

        foreach (
            CharacterAfflictions.STATUSTYPE
                statusType
            in statusTypes)
        {
            ActiveTimedStatus active =
                _timedStatuses[
                    statusType];

            if (now >= active.EndTime)
            {
                TrySetStatusValue(
                    afflictions,
                    statusType,
                    0f
                );

                _timedStatuses.Remove(
                    statusType
                );

                continue;
            }

            float value;

            if (active.ValueMode ==
                StatusValueMode.TimedDecay)
            {
                float progress =
                    Mathf.InverseLerp(
                        active.StartTime,
                        active.EndTime,
                        now
                    );

                value =
                    Mathf.Lerp(
                        active.StartAmount,
                        0f,
                        progress
                    );
            }
            else
            {
                value =
                    active.StartAmount;
            }

            TrySetStatusValue(
                afflictions,
                statusType,
                value
            );
        }
    }

    private void ClearTimedStatuses(
        Character character)
    {
        CharacterAfflictions? afflictions =
            FindAfflictions(
                character
            );

        if (afflictions is not null)
        {
            foreach (
                CharacterAfflictions.STATUSTYPE
                    statusType
                in _timedStatuses.Keys)
            {
                TrySetStatusValue(
                    afflictions,
                    statusType,
                    0f
                );
            }
        }

        _timedStatuses.Clear();
    }

    private void BuildStatusEffectList()
    {
        _specialEffects.Clear();

        foreach (
            CharacterAfflictions.STATUSTYPE
                statusType
            in Enum.GetValues(
                typeof(
                    CharacterAfflictions
                    .STATUSTYPE)))
        {
            string runtimeName =
                statusType.ToString();

            /*
             * Weight 使用独立的负重功能。
             */
            if (runtimeName.Equals(
                    "Weight",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            StatusValueMode valueMode =
                ResolveStatusValueMode(
                    runtimeName
                );

            bool showDuration =
                valueMode ==
                StatusValueMode.TimedConstant ||
                valueMode ==
                StatusValueMode.TimedDecay;

            float defaultAmount =
                ResolveDefaultAmount();

            _specialEffects.Add(
                new StatusEffectDefinition(
                    id:
                    $"status_{runtimeName}",

                    displayName:
                    TranslateStatusName(
                        runtimeName
                    ),

                    description:
                    BuildStatusDescription(
                        runtimeName,
                        valueMode
                    ),

                    kind:
                    StatusEffectKind.GameStatus,

                    valueMode:
                    valueMode,

                    showAmount:
                    true,

                    showDuration:
                    showDuration,

                    statusType:
                    statusType,

                    defaultAmount:
                    defaultAmount,

                    defaultDuration:
                    3f
                )
            );
        }

        Plugin.Log.LogInfo(
            $"Loaded {_specialEffects.Count} " +
            $"status effect definitions."
        );
    }

    private static StatusValueMode
        ResolveStatusValueMode(
            string runtimeName)
    {
        string name =
            NormalizeName(
                runtimeName
            );

        /*
         * 中毒写入一次，让游戏自然衰减。
         */
        if (name.Contains("poison"))
        {
            return StatusValueMode.NativeDecay;
        }

        /*
         * 明确具有持续时间的控制状态。
         */
        if (name.Contains("web") ||
            name.Contains("blind") ||
            name.Contains("snowblind") ||
            name.Contains("lowgravity") ||
            name.Contains("numb") ||
            name.Contains("sleep") ||
            name.Contains("stun"))
        {
            return StatusValueMode.TimedConstant;
        }

        /*
         * 如果后续发现某个状态适合平滑衰减，
         * 可在这里返回 TimedDecay。
         */
        return StatusValueMode.AmountOnly;
    }

    private static float ResolveDefaultAmount()
    {
        return 0.10f;
    }

    private static string
        BuildStatusDescription(
            string runtimeName,
            StatusValueMode valueMode)
    {
        string behavior =
            valueMode switch
            {
                StatusValueMode.AmountOnly =>
                    "只修改持有量，不使用菜单计时。",

                StatusValueMode.NativeDecay =>
                    "设置一次持有量，之后由游戏原生逻辑自然减少。",

                StatusValueMode.TimedConstant =>
                    "在指定秒数内保持该持有量，到期归零。",

                StatusValueMode.TimedDecay =>
                    "在指定秒数内逐渐衰减到零。",

                _ =>
                    string.Empty
            };

        return
            $"游戏状态：{runtimeName}。{behavior}";
    }

    private static string TranslateStatusName(
        string runtimeName)
    {
        return NormalizeName(runtimeName)
            switch
            {
                "poison" => "中毒",
                "cold" => "寒冷",
                "hot" => "炎热",
                "heat" => "炎热",
                "hunger" => "饥饿",
                "injury" => "受伤 / 扣血",
                "curse" => "诅咒",
                "drowsy" => "困倦",
                "sleep" => "睡眠",

                "web" => "蜘蛛网",
                "webbed" => "蜘蛛网",
                "webs" => "蜘蛛网",

                "blind" => "失明",
                "snowblind" => "雪盲",

                "numb" => "麻木",

                "lowgravity" => "低重力",

                "zombiebite" => "僵尸咬伤",

                "thorn" => "刺",
                "thorns" => "刺",
                "thron" => "刺",
                "throns" => "刺",

                "spore" => "孢子",
                "spores" => "孢子",

                "crab" => "奇异",
                "crabs" => "奇异",

                "stamina" => "体力",

                _ => runtimeName
            };
    }

    private static string NormalizeName(
        string value)
    {
        return value
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    private static float CalculateFinalAmount(
        float currentAmount,
        float inputAmount,
        StatusApplyMode mode)
    {
        return mode switch
        {
            StatusApplyMode.Add =>
                currentAmount +
                Mathf.Abs(inputAmount),

            StatusApplyMode.Subtract =>
                currentAmount -
                Mathf.Abs(inputAmount),

            StatusApplyMode.Set =>
                inputAmount,

            StatusApplyMode.Clear =>
                0f,

            _ =>
                inputAmount
        };
    }

    private static string
        BuildAppliedStatusMessage(
            StatusEffectDefinition effect,
            float amount,
            float duration,
            StatusApplyMode mode)
    {
        string operation =
            mode switch
            {
                StatusApplyMode.Add =>
                    "增加",

                StatusApplyMode.Subtract =>
                    "减少",

                StatusApplyMode.Set =>
                    "设为",

                StatusApplyMode.Clear =>
                    "清零",

                _ =>
                    "修改"
            };

        if (effect.ShowDuration)
        {
            return
                $"已将“{effect.DisplayName}”{operation} " +
                $"{amount:0.##}，持续 " +
                $"{duration:0.##} 秒。";
        }

        return
            $"已将“{effect.DisplayName}”{operation} " +
            $"{amount:0.##}。";
    }

    private static CharacterAfflictions?
        FindAfflictions(
            Character character)
    {
        CharacterAfflictions? result =
            character.GetComponent<
                CharacterAfflictions>();

        if (result is not null)
        {
            return result;
        }

        return character
            .GetComponentInChildren<
                CharacterAfflictions>(
                    true
                );
    }

    private static bool TrySetStatusValue(
        CharacterAfflictions afflictions,
        CharacterAfflictions.STATUSTYPE type,
        float value)
    {
        try
        {
            afflictions.SetStatus(
                type,
                value
            );

            return true;
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning(
                $"SetStatus({type}, {value}) failed: " +
                $"{exception.Message}"
            );

            return false;
        }
    }

    private static bool TryGetStatusValue(
        CharacterAfflictions afflictions,
        CharacterAfflictions.STATUSTYPE type,
        out float value)
    {
        value = 0f;

        /*
         * 优先使用游戏公开或私有的 GetStatus 方法。
         */
        if (GetStatusMethod is not null)
        {
            try
            {
                object? result =
                    GetStatusMethod.Invoke(
                        afflictions,
                        new object[]
                        {
                            type
                        }
                    );

                if (TryConvertFloat(
                        result,
                        out value))
                {
                    return true;
                }
            }
            catch
            {
                // 继续尝试字段和属性。
            }
        }

        return TryReadStatusFromMembers(
            afflictions,
            type,
            out value
        );
    }

    private static MethodInfo?
        FindGetStatusMethod()
    {
        Type type =
            typeof(CharacterAfflictions);

        return type
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            )
            .FirstOrDefault(
                method =>
                {
                    if (!method.Name.Equals(
                            "GetStatus",
                            StringComparison
                                .OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0]
                            .ParameterType !=
                        typeof(
                            CharacterAfflictions
                                .STATUSTYPE))
                    {
                        return false;
                    }

                    return
                        method.ReturnType ==
                            typeof(float) ||
                        method.ReturnType ==
                            typeof(double) ||
                        method.ReturnType ==
                            typeof(int);
                }
            );
    }

    private static bool
        TryReadStatusFromMembers(
            CharacterAfflictions afflictions,
            CharacterAfflictions.STATUSTYPE type,
            out float value)
    {
        value = 0f;

        Type runtimeType =
            afflictions.GetType();

        IEnumerable<MemberInfo> members =
            runtimeType
                .GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                )
                .Cast<MemberInfo>()
                .Concat(
                    runtimeType.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    )
                );

        foreach (MemberInfo member
                 in members)
        {
            object? container;

            try
            {
                container =
                    member switch
                    {
                        FieldInfo field =>
                            field.GetValue(
                                afflictions
                            ),

                        PropertyInfo property
                            when property.CanRead =>
                            property.GetValue(
                                afflictions
                            ),

                        _ => null
                    };
            }
            catch
            {
                continue;
            }

            if (container is null)
            {
                continue;
            }

            if (TryReadDictionaryValue(
                    container,
                    type,
                    out value))
            {
                return true;
            }

            if (TryReadIndexedValue(
                    container,
                    Convert.ToInt32(type),
                    out value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadDictionaryValue(
        object container,
        CharacterAfflictions.STATUSTYPE type,
        out float value)
    {
        value = 0f;

        if (container is not IDictionary dictionary)
        {
            return false;
        }

        try
        {
            if (!dictionary.Contains(type))
            {
                return false;
            }

            object? raw =
                dictionary[type];

            return TryConvertFloat(
                raw,
                out value
            );
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadIndexedValue(
        object container,
        int index,
        out float value)
    {
        value = 0f;

        if (index < 0)
        {
            return false;
        }

        try
        {
            if (container is Array array)
            {
                if (index >= array.Length)
                {
                    return false;
                }

                return TryConvertFloat(
                    array.GetValue(index),
                    out value
                );
            }

            if (container is IList list)
            {
                if (index >= list.Count)
                {
                    return false;
                }

                return TryConvertFloat(
                    list[index],
                    out value
                );
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool SetCharacterBoolean(
        Character character,
        PropertyInfo? property,
        FieldInfo? field,
        bool value)
    {
        try
        {
            if (property is not null &&
                property.CanWrite)
            {
                property.SetValue(
                    character,
                    value
                );

                return true;
            }

            if (field is not null)
            {
                field.SetValue(
                    character,
                    value
                );

                return true;
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning(
                $"Failed to set Character boolean: " +
                $"{exception.Message}"
            );
        }

        return false;
    }

    private static bool TryConvertFloat(
        object? raw,
        out float value)
    {
        try
        {
            if (raw is null)
            {
                value = 0f;
                return false;
            }

            value =
                Convert.ToSingle(
                    raw,
                    CultureInfo.InvariantCulture
                );

            return true;
        }
        catch
        {
            value = 0f;
            return false;
        }
    }

    private static bool IsFiniteNumber(
        float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }

    private static void LogRuntimeMembers()
    {
        Plugin.Log.LogInfo(
            $"Status runtime members: " +
            $"infiniteStamProperty=" +
            $"{InfiniteStaminaProperty is not null}, " +
            $"infiniteStamField=" +
            $"{InfiniteStaminaField is not null}, " +
            $"statusesLockedProperty=" +
            $"{StatusesLockedProperty is not null}, " +
            $"statusesLockedField=" +
            $"{StatusesLockedField is not null}, " +
            $"GetStatus=" +
            $"{GetStatusMethod is not null}."
        );

        Plugin.Log.LogInfo(
            "CharacterAfflictions.STATUSTYPE: " +
            string.Join(
                ", ",
                Enum.GetNames(
                    typeof(
                        CharacterAfflictions
                            .STATUSTYPE))
            )
        );
    }

    private void Fail(
        string message)
    {
        LastSucceeded = false;
        LastStatus = message;
    }

    private sealed class ActiveTimedStatus
    {
        internal ActiveTimedStatus(
            CharacterAfflictions.STATUSTYPE
                statusType,
            StatusValueMode valueMode,
            float startAmount,
            float startTime,
            float endTime)
        {
            StatusType = statusType;
            ValueMode = valueMode;

            StartAmount = startAmount;
            StartTime = startTime;
            EndTime = endTime;
        }

        internal CharacterAfflictions.STATUSTYPE
            StatusType { get; }

        internal StatusValueMode ValueMode { get; }

        internal float StartAmount { get; }

        internal float StartTime { get; }

        internal float EndTime { get; }
    }
}