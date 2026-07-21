using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class StatusPage : IMenuPage
{
    private bool _invincible;
    private bool _infiniteStamina;
    private bool _negativeStatusImmunity;

    public string Title => "状态";

    public void Draw(MenuStyles styles)
    {
        GUILayout.Label(
            "修改本地角色状态。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        _invincible = GUILayout.Toggle(
            _invincible,
            "无敌",
            styles.Toggle,
            GUILayout.Height(40f)
        );

        _infiniteStamina = GUILayout.Toggle(
            _infiniteStamina,
            "无限体力",
            styles.Toggle,
            GUILayout.Height(40f)
        );

        _negativeStatusImmunity = GUILayout.Toggle(
            _negativeStatusImmunity,
            "免疫负面状态",
            styles.Toggle,
            GUILayout.Height(40f)
        );
    }
}