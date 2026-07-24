using System.Globalization;
using P.E.A.K_MENU.Features.BlowDart;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class BlowDartPage :
    IMenuPage
{
    /*
     * UI 使用百分制。
     *
     * 底层默认值为 0.25，
     * 页面显示为 25。
     */
    private string _amountInput =
        "25";

    /*
     * 防止页面每帧重复调用 SetAmount。
     */
    private string _lastProcessedAmountInput =
        "25";

    private bool _amountInputInitialized;

    public string Title =>
        "吹箭";

    public void Draw(
        MenuStyles styles)
    {
        if (!BlowDartRuntime.IsInitialized)
        {
            GUILayout.Label(
                "吹箭功能尚未初始化。",
                styles.MutedLabel
            );

            return;
        }

        BlowDartService service =
            BlowDartRuntime.Service;

        EnsureAmountInputInitialized(
            service
        );

        GUILayout.Label(
            "仅房主使用时可稳定修改其他玩家的吹箭状态效果。",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.Label(
            "非房主使用时，效果可能只在本地触发或被游戏网络权限拒绝。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        GUILayout.Label(
            "拦截原版睡眠吹箭，并通过游戏状态 RPC 修改命中角色。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            "基础状态可作用于未安装本模组的队友。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        bool enabled =
            GUILayout.Toggle(
                service.Enabled,
                "启用吹箭效果替换",
                styles.Toggle,
                GUILayout.Height(40f)
            );

        if (enabled !=
            service.Enabled)
        {
            service.SetEnabled(
                enabled
            );
        }

        GUILayout.Space(10f);

        if (GUILayout.Button(
                "获取吹箭",
                styles.ActionButton,
                GUILayout.Height(40f)))
        {
            service.GiveBlowDart();
        }

        GUILayout.Space(14f);

        GUILayout.Label(
            "效果类型",
            styles.Label
        );

        GUILayout.Space(5f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "◀",
                styles.ActionButton,
                GUILayout.Width(48f),
                GUILayout.Height(40f)))
        {
            service.SelectPreviousEffect();
        }

        GUILayout.Label(
            BlowDartService
                .GetEffectDisplayName(
                    service.EffectType
                ),
            styles.Label,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(40f)
        );

        if (GUILayout.Button(
                "▶",
                styles.ActionButton,
                GUILayout.Width(48f),
                GUILayout.Height(40f)))
        {
            service.SelectNextEffect();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(14f);

        GUILayout.Label(
            "效果强度（百分比）",
            styles.Label
        );

        GUILayout.Space(4f);

        /*
         * UI 输入使用百分制。
         *
         * 输入 25：
         * 传给服务层的是 0.25。
         */
        string newAmountInput =
            GUILayout.TextField(
                _amountInput,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)
            );

        if (newAmountInput !=
            _amountInput)
        {
            _amountInput =
                newAmountInput;

            ProcessAmountInput(
                service
            );
        }

        GUILayout.Space(5f);

        GUILayout.Label(
            $"当前强度：" +
            $"{ToPercentage(service.Amount):0.###}%",
            styles.MutedLabel
        );

        GUILayout.Label(
            "输入百分制数值后会自动更新，例如输入 25 等于底层数值 0.25。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "建议使用 10～30，数值过高可能让目标立即倒下。",
            styles.MutedLabel
        );

        GUILayout.Space(14f);

        DrawEffectDescription(
            service,
            styles
        );

        GUILayout.Space(14f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );
    }

    private void EnsureAmountInputInitialized(
        BlowDartService service)
    {
        if (_amountInputInitialized)
        {
            return;
        }

        /*
         * 底层 0.25 转换为 UI 的 25。
         */
        _amountInput =
            ToPercentage(
                service.Amount
            ).ToString(
                "0.###",
                CultureInfo.InvariantCulture
            );

        _lastProcessedAmountInput =
            _amountInput;

        _amountInputInitialized =
            true;
    }

    private void ProcessAmountInput(
        BlowDartService service)
    {
        if (_amountInput ==
            _lastProcessedAmountInput)
        {
            return;
        }

        /*
         * 输入框暂时为空时不报错。
         */
        if (string.IsNullOrWhiteSpace(
                _amountInput))
        {
            return;
        }

        string trimmed =
            _amountInput.Trim();

        /*
         * 用户可能还在输入小数，
         * 这些临时内容不立即报错。
         */
        if (trimmed == "-" ||
            trimmed == "+" ||
            trimmed == "." ||
            trimmed == "," ||
            trimmed == "-." ||
            trimmed == "-," ||
            trimmed == "+." ||
            trimmed == "+,")
        {
            return;
        }

        /*
         * 允许用户输入末尾百分号。
         *
         * 25 和 25% 都会被识别为 25%。
         */
        if (trimmed.EndsWith(
                "%"))
        {
            trimmed =
                trimmed
                    .Substring(
                        0,
                        trimmed.Length - 1
                    )
                    .Trim();
        }

        bool parsed =
            float.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float percentage
            ) ||
            float.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out percentage
            );

        if (!parsed)
        {
            return;
        }

        /*
         * UI 百分制转换回底层 0～1 数值。
         *
         * 25 ÷ 100 = 0.25
         */
        float internalAmount =
            FromPercentage(
                percentage
            );

        _lastProcessedAmountInput =
            _amountInput;

        service.SetAmount(
            internalAmount
        );
    }

    private static float ToPercentage(
        float internalValue)
    {
        return
            internalValue *
            100f;
    }

    private static float FromPercentage(
        float percentage)
    {
        return
            percentage /
            100f;
    }

    private static void DrawEffectDescription(
        BlowDartService service,
        MenuStyles styles)
    {
        string description =
            service.EffectType switch
            {
                BlowDartEffectType.Original =>
                    "完全使用游戏原版睡眠吹箭。",

                BlowDartEffectType.Injury =>
                    "命中后增加目标的受伤状态。",

                BlowDartEffectType.Poison =>
                    "命中后增加目标的中毒状态。",

                BlowDartEffectType.Cold =>
                    "命中后增加目标的寒冷状态。",

                BlowDartEffectType.Hot =>
                    "命中后增加目标的炎热状态。",

                BlowDartEffectType.Hunger =>
                    "命中后增加目标的饥饿状态。",

                BlowDartEffectType.Drowsy =>
                    "命中后增加目标的困倦状态。",

                BlowDartEffectType.Curse =>
                    "命中后增加目标的诅咒状态。",

                _ =>
                    string.Empty
            };

        GUILayout.Label(
            description,
            styles.MutedLabel
        );
    }
}