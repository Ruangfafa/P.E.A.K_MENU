using System;
using System.Globalization;
using P.E.A.K_MENU.Features.BlowDart;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class BlowDartPage :
    IMenuPage
{
    private readonly ShortcutRebindControl _shortcut = new();
    private bool _effectDropdownOpen;
    private bool _inputsInitialized;
    private string _amountInput = "20";
    private string _durationInput = "5";

    public string Title => "吹箭";

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

        EnsureInputsInitialized(service);
        _shortcut.CaptureEvent();

        if (GUILayout.Button("将当前效果作用于自己", styles.ActionButton)) service.ApplyToSelf();

        GUILayout.Label(
            "设置只影响你自己发射的吹箭；未安装 Mod 的玩家仍可正常受击。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            "其他玩家发射的吹箭保持游戏原有效果。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "选择“无效果（原版）”时，你发射的吹箭同样使用原版效果。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        using (new GUILayout.HorizontalScope(GUILayout.Height(38f)))
        {
            if (GUILayout.Button("获取吹箭", styles.ActionButton,
                    GUILayout.Height(38f), GUILayout.ExpandWidth(true)))
            {
                service.GiveBlowDart();
            }
            _shortcut.DrawButtons(Input.FeatureShortcutAction.GiveBlowDart, styles);
        }

        GUILayout.Space(14f);

        GUILayout.Label(
            "吹箭效果",
            styles.Label
        );

        GUILayout.Space(4f);

        if (GUILayout.Button(
                BlowDartService.GetDisplayName(
                    service.EffectType
                ) +
                (_effectDropdownOpen ? " ▴" : " ▾"),
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            _effectDropdownOpen =
                !_effectDropdownOpen;
        }

        if (_effectDropdownOpen)
        {
            foreach (BlowDartEffectType effectType
                     in Enum.GetValues(
                         typeof(BlowDartEffectType)
                     ))
            {
                if (effectType == BlowDartEffectType.Whirlwind) continue;
                GUIStyle style =
                    effectType == service.EffectType
                        ? styles.ThemeButtonSelected
                        : styles.ThemeButton;

                if (!GUILayout.Button(
                        BlowDartService.GetDisplayName(
                            effectType
                        ),
                        style,
                        GUILayout.Height(32f)))
                {
                    continue;
                }

                service.SetEffectType(effectType);
                _effectDropdownOpen = false;
            }
        }

        if (BlowDartService.UsesAmount(
                service.EffectType))
        {
            DrawAmountInput(
                service,
                styles
            );
        }
        else if (service.EffectType ==
                 BlowDartEffectType.Whirlwind)
        {
            DrawDurationInput(
                service,
                styles
            );
        }

        GUILayout.Space(14f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );
    }

    private void DrawAmountInput(BlowDartService service, MenuStyles styles)
    {
        GUILayout.Space(14f);
        GUILayout.Label($"赋予值：{service.AmountPercent:0.#}%（−200～200，每 2.5 一档）", styles.Label);
        float next = Mathf.Round(GUILayout.HorizontalSlider(service.AmountPercent, -200f, 200f) / 2.5f) * 2.5f;
        if (next != service.AmountPercent) service.SetAmountPercent(next);
        if (service.AmountPercent < 0f)
        {
            string notice = !Photon.Pun.PhotonNetwork.InRoom
                ? "当前不在房间中，无法确认房主是否支持状态扣减。"
                : Photon.Pun.PhotonNetwork.IsMasterClient
                    ? "当前为房主：负数效果由你直接执行状态扣减。"
                    : P.E.A.K_MENU.Features.Teleport.TeleportNetwork.HostSupportsReduction
                        ? "已检测到房主支持新版扣减协议：对其他玩家的负数效果将请求房主代执行。"
                        : "未检测到房主支持新版扣减协议：无法扣减其他玩家状态，请房主安装新版模组。";
            GUILayout.Label(notice, styles.NoticeLabel);
            GUILayout.Label("作用于自己时使用本地扣减，无需房主代执行。", styles.NoticeLabel);
        }
    }

    private void DrawDurationInput(
        BlowDartService service,
        MenuStyles styles)
    {
        GUILayout.Space(14f);

        GUILayout.Label(
            "持续时间（秒）",
            styles.Label
        );

        string nextInput = GUILayout.TextField(
            _durationInput,
            GUILayout.Height(36f),
            GUILayout.ExpandWidth(true)
        );

        if (nextInput != _durationInput)
        {
            _durationInput = nextInput;
            ProcessDurationInput(service);
        }

        GUILayout.Label(
            $"当前：{service.DurationSeconds:0.##} 秒",
            styles.MutedLabel
        );
    }

    private void EnsureInputsInitialized(
        BlowDartService service)
    {
        if (_inputsInitialized)
        {
            return;
        }

        _amountInput =
            service.AmountPercent.ToString(
                CultureInfo.InvariantCulture
            );

        _durationInput =
            service.DurationSeconds.ToString(
                "0.##",
                CultureInfo.InvariantCulture
            );

        _inputsInitialized = true;
    }

    private void ProcessAmountInput(
        BlowDartService service)
    {
        if (int.TryParse(
                _amountInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value))
        {
            service.SetAmountPercent(value);
        }
    }

    private void ProcessDurationInput(
        BlowDartService service)
    {
        bool parsed = float.TryParse(
                          _durationInput,
                          NumberStyles.Float,
                          CultureInfo.InvariantCulture,
                          out float value
                      ) ||
                      float.TryParse(
                          _durationInput,
                          NumberStyles.Float,
                          CultureInfo.CurrentCulture,
                          out value
                      );

        if (parsed)
        {
            service.SetDurationSeconds(value);
        }
    }
}
