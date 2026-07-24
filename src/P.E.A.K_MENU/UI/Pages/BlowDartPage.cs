using System.Globalization;
using P.E.A.K_MENU.Features.BlowDart;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class BlowDartPage :
    IMenuPage
{
    private string _amountInput =
        "0.25";

    /*
     * 防止页面每帧重复调用 SetAmount。
     */
    private string _lastProcessedAmountInput =
        "0.25";

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

        /*
         * 获取吹箭按钮。
         */
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
            "效果强度",
            styles.Label
        );

        GUILayout.Space(4f);

        /*
         * 不再显示“确定”按钮。
         *
         * 输入文本发生变化时立即尝试更新。
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
            $"当前强度：{service.Amount:0.###}",
            styles.MutedLabel
        );

        GUILayout.Label(
            "输入有效数值后会自动更新，无需点击确定。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "建议使用 0.10～0.30，状态值过高可能让目标立即倒下。",
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

        _amountInput =
            service.Amount.ToString(
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
         *
         * 用户可能正在删除旧数值，
         * 准备输入一个新数值。
         */
        if (string.IsNullOrWhiteSpace(
                _amountInput))
        {
            return;
        }

        /*
         * 用户输入负数时，刚输入一个减号也不报错。
         */
        string trimmed =
            _amountInput.Trim();

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

        bool parsed =
            float.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float amount
            ) ||
            float.TryParse(
                trimmed,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out amount
            );

        if (!parsed)
        {
            /*
             * 输入尚未完成时不覆盖当前有效强度，
             * 也不持续输出错误信息。
             */
            return;
        }

        _lastProcessedAmountInput =
            _amountInput;

        service.SetAmount(
            amount
        );
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