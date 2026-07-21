using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class ItemSpawnerPage : IMenuPage
{
    public string Title => "物品生成";

    public void Draw(MenuStyles styles)
    {
        GUILayout.Label(
            "选择或搜索一个物品并生成。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        if (GUILayout.Button(
                "生成测试物品",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            Plugin.Log.LogInfo(
                "Item spawn test clicked."
            );
        }
    }
}