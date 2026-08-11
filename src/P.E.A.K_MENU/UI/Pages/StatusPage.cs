using System.Globalization;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.Input;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class StatusPage :
    IMenuPage
{
    private Vector2 _pageScroll;

    private string _weightInput =
        "0";

    private readonly ShortcutRebindControl
        _shortcutRebind = new();

    public string Title =>
        "状态";

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

        DrawRevive(
            service,
            styles
        );

        GUILayout.Space(14f);

        DrawWeight(
            service,
            styles
        );

        GUILayout.Space(16f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );

        GUILayout.EndScrollView();

        _shortcutRebind.CaptureEvent();
    }

    private void DrawInvincibility(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "角色保护",
            styles.Label
        );

        GUILayout.Space(4f);

        /*
         * 实际飞行期间，
         * 无敌状态由飞行功能管理。
         */
        GUILayout.BeginHorizontal();

        GUI.enabled =
            !service.FlightProtectionLock;

        bool invincible =
            GUILayout.Toggle(
                service.Invincible,
                "启用无敌",
                styles.Toggle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)
            );

        GUI.enabled =
            true;

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleInvincibility,
            styles
        );

        GUILayout.EndHorizontal();

        if (!service.FlightProtectionLock &&
            invincible !=
            service.Invincible)
        {
            service.SetInvincible(
                invincible
            );
        }

        /*
         * 防击退只有在无敌开启，
         * 且没有被飞行功能锁定时才能修改。
         */
        GUILayout.BeginHorizontal();

        GUI.enabled =
            service.Invincible &&
            !service.FlightProtectionLock;

        bool antiKnockback =
            GUILayout.Toggle(
                service.AntiKnockback,
                "阻止击退、摔倒与外力",
                styles.Toggle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)
            );

        GUI.enabled =
            true;

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleAntiKnockback,
            styles
        );

        GUILayout.EndHorizontal();

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
                "实际飞行期间，角色保护状态由飞行功能管理。",
                styles.MutedLabel
            );

            return;
        }

        GUILayout.Label(
            "防击退需要先开启无敌。",
            styles.MutedLabel
        );
    }

    private void DrawInfiniteStamina(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "体力",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.BeginHorizontal();

        bool enabled =
            GUILayout.Toggle(
                service.InfiniteStamina,
                "无限体力",
                styles.Toggle,
                GUILayout.Height(40f),
                GUILayout.ExpandWidth(true)
            );

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleInfiniteStamina,
            styles
        );

        GUILayout.EndHorizontal();

        if (enabled !=
            service.InfiniteStamina)
        {
            service.SetInfiniteStamina(
                enabled
            );
        }
    }

    private void DrawRevive(
        StatusService service,
        MenuStyles styles)
    {
        GUILayout.Label(
            "死亡恢复",
            styles.Label
        );

        GUILayout.Space(4f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "复活自己",
                styles.ActionButton,
                GUILayout.Height(42f),
                GUILayout.ExpandWidth(true)))
        {
            service.ReviveSelf();
        }

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ReviveSelf,
            styles
        );

        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        GUILayout.Label(
            "仅在本地角色已经死亡后生效，复活位置优先使用幽灵当前位置。",
            styles.MutedLabel
        );
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

        GUILayout.BeginHorizontal();

        bool enabled =
            GUILayout.Toggle(
                service.WeightOverrideEnabled,
                "启用负重覆盖",
                styles.Toggle,
                GUILayout.Height(38f),
                GUILayout.ExpandWidth(true)
            );

        _shortcutRebind.DrawButtons(
            FeatureShortcutAction.ToggleWeightOverride,
            styles
        );

        GUILayout.EndHorizontal();

        if (enabled !=
            service.WeightOverrideEnabled)
        {
            service.SetWeightOverride(
                enabled
            );
        }

        GUILayout.Space(4f);

        GUI.enabled =
            service.WeightOverrideEnabled;

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
            ApplyWeight(
                service
            );
        }

        GUILayout.EndHorizontal();

        GUI.enabled =
            true;

        GUILayout.Label(
            "输入 0 可实现无负重。",
            styles.MutedLabel
        );
    }

    private void ApplyWeight(
        StatusService service)
    {
        bool parsed =
            float.TryParse(
                _weightInput,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float weight
            ) ||
            float.TryParse(
                _weightInput,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out weight
            );

        if (!parsed)
        {
            service.SetInputError(
                "负重格式无效。"
            );

            return;
        }

        service.SetWeight(
            weight
        );

        _weightInput =
            weight.ToString(
                "0.###",
                CultureInfo.InvariantCulture
            );
    }
}
