using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class TeleportPage : IMenuPage
{
    public string Title => "传送";

    public void Draw(MenuStyles styles)
    {
        GUILayout.Label(
            "选择目标或指定位置进行传送。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        if (GUILayout.Button(
                "传送到玩家",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            Plugin.Log.LogInfo(
                "Teleport to player clicked."
            );
        }

        if (GUILayout.Button(
                "传送到坐标",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            Plugin.Log.LogInfo(
                "Teleport to position clicked."
            );
        }
    }
}