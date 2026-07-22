using System.Collections.Generic;
using System.Globalization;
using P.E.A.K_MENU.Features.Status;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class StatusPage :
    IMenuPage
{
    private const float SpecialEffectsHeight =
        400f;

    private readonly Dictionary<string, string>
        _amountInputs = new();

    private readonly Dictionary<string, string>
        _durationInputs = new();

    private readonly Dictionary<
        string,
        StatusApplyMode>
        _applyModes = new();

    private Vector2 _pageScroll;
    private Vector2 _specialEffectsScroll;

    private string _weightInput = "0";

    public string Title => "状态";

    public void Draw(
        MenuStyles styles)
    {
        if (!StatusRuntime.IsInitialized)
        {
            GUILayout.Label(
                "状态功能尚未初始化。",
                styles.MutedLabel
            );

            return;
        }

        StatusService service =
            StatusRuntime.Service;

        EnsureInputsExist(
            service
        );

        _pageScroll =
            GUILayout.BeginScrollView(
                _pageScroll,
                false,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        DrawInvincibility(
            service,
            styles
        );

        GUILayout.Space(14f);

        DrawInfiniteStamina(
            service,
            styles
        );

        GUILayout.Space(14f);

        DrawClearEffects(
            service,
            styles
        );

        GUILayout.Space(14f);

        DrawWeight(
            service,
            styles
        );

        GUILayout.Space(16f);

        DrawSpecialEffects(
            service,
            styles
        );

        GUILayout.Space(12f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );

        GUILayout.EndScrollView();
    }

    private static void DrawInvincibility(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "无敌",
            styles.Label
        );

        GUILayout.Space(4f);

        /*
         * 飞行总开关开启时，
         * 禁止用户修改无敌。
         */
        GUI.enabled =
            !service.FlightProtectionLock;

        bool invincible =
            GUILayout.Toggle(
                service.Invincible,
                "启用无敌",
                styles.Toggle,
                GUILayout.Height(40f)
            );

        GUI.enabled = true;

        if (!service.FlightProtectionLock &&
            invincible !=
            service.Invincible)
        {
            service.SetInvincible(
                invincible
            );
        }

        /*
         * 防击退只有在无敌开启、
         * 且没有被飞行锁定时才允许修改。
         */
        GUI.enabled =
            service.Invincible &&
            !service.FlightProtectionLock;

        bool antiKnockback =
            GUILayout.Toggle(
                service.AntiKnockback,
                "阻止击退、摔倒与外力",
                styles.Toggle,
                GUILayout.Height(40f)
            );

        GUI.enabled = true;

        if (!service.FlightProtectionLock &&
            antiKnockback !=
            service.AntiKnockback)
        {
            service.SetAntiKnockback(
                antiKnockback
            );
        }

        if (service.FlightProtectionLock)
        {
            GUILayout.Label(
                "飞行总开关开启期间，无敌与防击退已被强制开启并锁定。",
                styles.MutedLabel
            );

            return;
        }

        GUILayout.Label(
            "附加保护默认开启，关闭后仍保留死亡保护。",
            styles.MutedLabel
        );
    }

    private static void DrawInfiniteStamina(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "体力",
            styles.Label
        );

        GUILayout.Space(4f);

        bool enabled =
            GUILayout.Toggle(
                service.InfiniteStamina,
                "无限体力",
                styles.Toggle,
                GUILayout.Height(40f)
            );

        if (enabled !=
            service.InfiniteStamina)
        {
            service.SetInfiniteStamina(
                enabled
            );
        }
    }

    private static void DrawClearEffects(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "状态清理",
            styles.Label
        );

        GUILayout.Space(4f);

        if (GUILayout.Button(
                "清除所有负面效果",
                styles.ActionButton,
                GUILayout.Height(40f)))
        {
            service.ClearNegativeEffects();
        }
    }

    private void DrawWeight(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "负重重量",
            styles.Label
        );

        GUILayout.Space(4f);

        bool enabled =
            GUILayout.Toggle(
                service.WeightOverrideEnabled,
                "启用负重覆盖",
                styles.Toggle,
                GUILayout.Height(38f)
            );

        if (enabled !=
            service.WeightOverrideEnabled)
        {
            service.SetWeightOverride(
                enabled
            );
        }

        GUILayout.BeginHorizontal();

        _weightInput =
            GUILayout.TextField(
                _weightInput,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)
            );

        if (GUILayout.Button(
                "确定",
                styles.ActionButton,
                GUILayout.Width(76f),
                GUILayout.Height(36f)))
        {
            if (TryParseNumber(
                    _weightInput,
                    "负重",
                    out float weight))
            {
                service.SetWeight(
                    weight
                );
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Label(
            "输入 0 可实现无负重。",
            styles.MutedLabel
        );
    }

    private void DrawSpecialEffects(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "特殊状态合集",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.Label(
            "每种状态会根据自身机制显示持有量、持续时间或两者。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        _specialEffectsScroll =
            GUILayout.BeginScrollView(
                _specialEffectsScroll,
                false,
                true,
                GUILayout.Height(
                    SpecialEffectsHeight
                ),
                GUILayout.ExpandWidth(true)
            );

        foreach (StatusEffectDefinition effect
                 in service.SpecialEffects)
        {
            DrawEffectPanel(
                service,
                effect,
                styles
            );

            GUILayout.Space(10f);
        }

        GUILayout.EndScrollView();
    }

    private void DrawEffectPanel(
        StatusService service,
        StatusEffectDefinition effect,
        MenuStyles styles)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        GUILayout.Label(
            effect.DisplayName,
            styles.Label,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(30f)
        );

        if (effect.StatusType.HasValue &&
            service.TryReadStatus(
                effect,
                out float currentValue))
        {
            GUILayout.Label(
                $"当前：{currentValue:0.###}",
                styles.MutedLabel,
                GUILayout.Width(110f),
                GUILayout.Height(30f)
            );
        }

        GUILayout.EndHorizontal();

        GUILayout.Label(
            effect.Description,
            styles.MutedLabel
        );

        GUILayout.Space(5f);

        if (effect.ShowAmount)
        {
            DrawAmountInput(
                effect,
                styles
            );

            GUILayout.Space(4f);
        }

        if (effect.ShowDuration)
        {
            DrawDurationInput(
                effect,
                styles
            );

            GUILayout.Space(4f);
        }

        if (effect.Kind ==
            StatusEffectKind.GameStatus)
        {
            DrawApplyModeButtons(
                effect,
                styles
            );

            GUILayout.Space(5f);
        }

        if (GUILayout.Button(
                BuildApplyButtonText(
                    effect
                ),
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            ApplyEffect(
                service,
                effect
            );
        }

        GUILayout.EndVertical();
    }

    private void DrawAmountInput(
        StatusEffectDefinition effect,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "持有量",
            styles.MutedLabel,
            GUILayout.Width(72f),
            GUILayout.Height(34f)
        );

        _amountInputs[
            effect.Id] =
                GUILayout.TextField(
                    _amountInputs[
                        effect.Id],
                    GUILayout.Height(34f),
                    GUILayout.ExpandWidth(true)
                );

        GUILayout.EndHorizontal();
    }

    private void DrawDurationInput(
        StatusEffectDefinition effect,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "持续秒数",
            styles.MutedLabel,
            GUILayout.Width(72f),
            GUILayout.Height(34f)
        );

        _durationInputs[
            effect.Id] =
                GUILayout.TextField(
                    _durationInputs[
                        effect.Id],
                    GUILayout.Height(34f),
                    GUILayout.ExpandWidth(true)
                );

        GUILayout.EndHorizontal();
    }

    private void DrawApplyModeButtons(
        StatusEffectDefinition effect,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "应用方式",
            styles.MutedLabel,
            GUILayout.Width(72f),
            GUILayout.Height(32f)
        );

        DrawApplyModeButton(
            effect,
            StatusApplyMode.Add,
            "增加",
            styles
        );

        DrawApplyModeButton(
            effect,
            StatusApplyMode.Subtract,
            "减少",
            styles
        );

        DrawApplyModeButton(
            effect,
            StatusApplyMode.Set,
            "设为",
            styles
        );

        DrawApplyModeButton(
            effect,
            StatusApplyMode.Clear,
            "清零",
            styles
        );

        GUILayout.EndHorizontal();
    }

    private void DrawApplyModeButton(
        StatusEffectDefinition effect,
        StatusApplyMode mode,
        string text,
        MenuStyles styles)
    {
        bool selected =
            _applyModes[
                effect.Id] == mode;

        string buttonText =
            selected
                ? $"● {text}"
                : text;

        if (GUILayout.Button(
                buttonText,
                styles.ActionButton,
                GUILayout.Height(32f),
                GUILayout.ExpandWidth(true)))
        {
            _applyModes[
                effect.Id] =
                    mode;
        }
    }

    private void ApplyEffect(
        StatusService service,
        StatusEffectDefinition effect)
    {
        float amount =
            effect.DefaultAmount;

        float duration =
            effect.DefaultDuration;

        if (effect.ShowAmount)
        {
            if (!TryParseNumber(
                    _amountInputs[
                        effect.Id],
                    $"{effect.DisplayName}持有量",
                    out amount))
            {
                return;
            }
        }

        if (effect.ShowDuration)
        {
            if (!TryParseNumber(
                    _durationInputs[
                        effect.Id],
                    $"{effect.DisplayName}持续时间",
                    out duration))
            {
                return;
            }

            if (duration <= 0f)
            {
                service.SetInputError(
                    "持续时间必须大于 0 秒。"
                );

                return;
            }
        }

        StatusApplyMode applyMode =
            _applyModes[
                effect.Id];

        service.ApplySpecialEffect(
            effect,
            amount,
            duration,
            applyMode
        );
    }

    private void EnsureInputsExist(
        StatusService service)
    {
        foreach (StatusEffectDefinition effect
                 in service.SpecialEffects)
        {
            if (!_amountInputs.ContainsKey(
                    effect.Id))
            {
                _amountInputs[
                    effect.Id] =
                        effect
                            .DefaultAmount
                            .ToString(
                                "0.###",
                                CultureInfo
                                    .InvariantCulture
                            );
            }

            if (!_durationInputs.ContainsKey(
                    effect.Id))
            {
                _durationInputs[
                    effect.Id] =
                        effect
                            .DefaultDuration
                            .ToString(
                                "0.###",
                                CultureInfo
                                    .InvariantCulture
                            );
            }

            if (!_applyModes.ContainsKey(
                    effect.Id))
            {
                _applyModes[
                        effect.Id] =
                    StatusApplyMode.Set;
            }
        }
    }

    private static string
        BuildApplyButtonText(
            StatusEffectDefinition effect)
    {
        return "应用状态";
    }

    private bool TryParseNumber(
        string text,
        string fieldName,
        out float value)
    {
        bool parsed =
            float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value
            ) ||
            float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value
            );

        if (!parsed)
        {
            StatusRuntime
                .Service
                .SetInputError(
                    $"{fieldName}格式无效。"
                );

            return false;
        }

        return true;
    }
}