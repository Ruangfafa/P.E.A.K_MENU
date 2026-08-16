using P.E.A.K_MENU.Features.Flight;
using P.E.A.K_MENU.Input;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class FlightPage :
    IMenuPage
{
    private static readonly float[]
        GravityCalibrationSteps =
        {
            -9f,
            -3f,
            -1f,
            1f,
            3f,
            9f
        };

    private bool _showGravityCalibration;

    private readonly ShortcutRebindControl
        _shortcutRebind = new();

    public string Title =>
        "飞行";

    public void Draw(
        MenuStyles styles)
    {
        if (!FlightRuntime.IsInitialized)
        {
            GUILayout.Label(
                "飞行功能尚未初始化。",
                styles.MutedLabel
            );

            return;
        }

        FlightService service =
            FlightRuntime.Service;

        GUILayout.Label(
            "飞行总开关仅启用快捷键；进入实际飞行后才会开启无敌与防击退。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            "退出实际飞行时会恢复此前的无敌与防击退状态。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        GUILayout.BeginHorizontal();

        bool enabled =
            GUILayout.Toggle(
                service.Enabled,
                "启用飞行总开关",
                styles.Toggle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)
            );

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleFlightSystem,
            styles
        );

        GUILayout.EndHorizontal();

        if (enabled !=
            service.Enabled)
        {
            service.SetEnabled(
                enabled
            );
        }

        GUI.enabled = service.Enabled;

        GUILayout.BeginHorizontal();
        GUILayout.Space(24f);

        bool doubleTapEnabled =
            GUILayout.Toggle(
                FeatureInputSettings
                    .DoubleTapFlightEnabled,
                "允许双击空格进入或退出实际飞行",
                styles.Toggle,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)
            );

        GUILayout.EndHorizontal();

        if (doubleTapEnabled !=
            FeatureInputSettings.DoubleTapFlightEnabled)
        {
            FeatureInputSettings.DoubleTapFlightEnabled =
                doubleTapEnabled;
        }

        GUI.enabled = true;

        GUILayout.Space(12f);

        GUILayout.Label(
            "WASD 移动方式",
            styles.Label
        );

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "随视角飞行",
                service.HorizontalWasdMovement
                    ? styles.ThemeButton
                    : styles.ThemeButtonSelected,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)))
        {
            service.SetHorizontalWasdMovement(
                false
            );
        }

        if (GUILayout.Button(
                "水平平移",
                service.HorizontalWasdMovement
                    ? styles.ThemeButtonSelected
                    : styles.ThemeButton,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)))
        {
            service.SetHorizontalWasdMovement(
                true
            );
        }

        GUILayout.EndHorizontal();

        GUILayout.Label(
            service.HorizontalWasdMovement
                ? "WASD 只改变水平位置，高度由空格和 Ctrl 控制。"
                : "W/S 跟随视角俯仰，A/D 保持水平移动。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        GUILayout.Label(
            "飞行速度",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.Label(
            $"当前速度：{service.FlightSpeed:0.##}",
            styles.MutedLabel
        );

        GUILayout.Label(
            "调整范围为 16–255；实际飞行时，每次提高或降低 16 点。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        DrawSpeedShortcutRow(
            service,
            styles,
            increase: true
        );

        DrawSpeedShortcutRow(
            service,
            styles,
            increase: false
        );

        GUILayout.Space(12f);

        DrawGravityCalibration(
            service,
            styles
        );

        GUILayout.Space(12f);

        GUILayout.Label(
            "实际飞行状态",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.Label(
            ResolveFlightStatus(
                service
            ),
            service.ActivelyFlying
                ? styles.Label
                : styles.MutedLabel
        );

        GUILayout.BeginHorizontal();

        GUI.enabled = service.Enabled;

        if (GUILayout.Button(
                service.ActivelyFlying
                    ? "退出实际飞行"
                    : "进入实际飞行",
                styles.ActionButton,
                GUILayout.Height(38f),
                GUILayout.ExpandWidth(true)))
        {
            service.ToggleActiveFlight();
        }

        GUI.enabled =
            true;

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleActiveFlight,
            styles
        );

        GUILayout.EndHorizontal();

        GUILayout.Space(12f);

        GUILayout.Label(
            "操作：WASD 按所选方式移动，空格上升，Ctrl 下降，Shift 加速。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "启用子项后，双击空格可进入或退出实际飞行；进入时速度重置为 16。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "退出实际飞行后：保留正常重力，并限制 2 秒最大下落速度。",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );

        _shortcutRebind.CaptureEvent();
    }

    private void DrawSpeedShortcutRow(
        FlightService service,
        MenuStyles styles,
        bool increase)
    {
        GUILayout.BeginHorizontal();

        GUI.enabled =
            service.Enabled &&
            service.ActivelyFlying;

        if (GUILayout.Button(
                increase
                    ? "提高飞行速度"
                    : "降低飞行速度",
                styles.ActionButton,
                GUILayout.Height(38f),
                GUILayout.ExpandWidth(true)))
        {
            service.AdjustFlightSpeed(
                increase ? 1f : -1f
            );
        }

        GUI.enabled = true;

        _shortcutRebind.DrawButtons(
            increase
                ? FeatureShortcutAction
                    .IncreaseFlightSpeed
                : FeatureShortcutAction
                    .DecreaseFlightSpeed,
            styles
        );

        GUILayout.EndHorizontal();
    }

    private void DrawGravityCalibration(
        FlightService service,
        MenuStyles styles)
    {
        if (GUILayout.Button(
                _showGravityCalibration
                    ? "高级校准 ▾"
                    : "高级校准 ▸",
                styles.ActionButton,
                GUILayout.Height(32f)))
        {
            _showGravityCalibration =
                !_showGravityCalibration;
        }

        if (!_showGravityCalibration)
        {
            return;
        }

        GUILayout.Space(6f);

        GUILayout.Label(
            $"浮空重力校准：{service.HoverDownForce:0.##}",
            styles.Label
        );

        GUILayout.Label(
            "增加 = 加强向下补偿；角色仍在缓慢上浮时使用。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "减少 = 减弱向下补偿；角色正在缓慢下沉时使用。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "仅可在实际飞行状态下校准；可按不同步长快速微调。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();

        GUI.enabled =
            service.Enabled &&
            service.ActivelyFlying;

        foreach (float step in
                 GravityCalibrationSteps)
        {
            string label =
                step > 0f
                    ? $"+{step:0}"
                    : $"{step:0}";

            if (GUILayout.Button(
                    label,
                    styles.ActionButton,
                    GUILayout.Height(34f),
                    GUILayout.ExpandWidth(true)))
            {
                service.AdjustHoverDownForce(step);
            }
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button(
                $"恢复默认（{FlightService.DefaultHoverDownForce:0.##}）",
                styles.ActionButton,
                GUILayout.Height(34f)))
        {
            service.ResetHoverDownForce();
        }
    }

    private static string ResolveFlightStatus(
        FlightService service)
    {
        if (!service.Enabled)
        {
            return
                "飞行总开关未开启";
        }

        if (service.ActivelyFlying)
        {
            return
                "正在飞行";
        }

        return
            "正常状态 / 等待双击空格";
    }

}
