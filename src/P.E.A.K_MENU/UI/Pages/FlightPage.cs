using System.Globalization;
using P.E.A.K_MENU.Features.Flight;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class FlightPage :
    IMenuPage
{
    private string _speedInput =
        "16";

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
            "开启飞行总开关后，将强制开启无敌，并暂时关闭防击退。",
            styles.MutedLabel
        );

        GUILayout.Space(6f);

        GUILayout.Label(
            "双击空格、滚轮调速和退出缓降均为固定功能。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        bool enabled =
            GUILayout.Toggle(
                service.Enabled,
                "启用飞行总开关",
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

        GUILayout.Space(12f);

        GUILayout.Label(
            "飞行速度",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.BeginHorizontal();

        _speedInput =
            GUILayout.TextField(
                _speedInput,
                GUILayout.Height(36f),
                GUILayout.ExpandWidth(true)
            );

        if (GUILayout.Button(
                "确定",
                styles.ActionButton,
                GUILayout.Width(76f),
                GUILayout.Height(36f)))
        {
            ApplySpeed(
                service
            );
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8f);

        GUILayout.Label(
            $"当前速度：{service.FlightSpeed:0.##}",
            styles.MutedLabel
        );

        GUILayout.Label(
            "实际飞行时，滚轮向上提高 5 点，滚轮向下降低 5 点。",
            styles.MutedLabel
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

        GUI.enabled =
            service.Enabled;

        if (GUILayout.Button(
                service.ActivelyFlying
                    ? "退出实际飞行"
                    : "进入实际飞行",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            service.ToggleActiveFlight();
        }

        GUI.enabled =
            true;

        GUILayout.Space(12f);

        GUILayout.Label(
            "操作：WASD 移动，空格上升，Ctrl 下降，Shift 加速。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "双击空格可随时进入或退出实际飞行。",
            styles.MutedLabel
        );

        GUILayout.Label(
            "退出实际飞行后：前 1 秒无重力，总计 5 秒缓降。",
            styles.MutedLabel
        );

        GUILayout.Space(8f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );
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

    private void ApplySpeed(
        FlightService service)
    {
        bool parsed =
            float.TryParse(
                _speedInput,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float speed
            ) ||
            float.TryParse(
                _speedInput,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out speed
            );

        if (!parsed)
        {
            service.SetFlightSpeed(
                float.NaN
            );

            return;
        }

        service.SetFlightSpeed(
            speed
        );

        _speedInput =
            service.FlightSpeed.ToString(
                "0.##",
                CultureInfo.InvariantCulture
            );
    }
}