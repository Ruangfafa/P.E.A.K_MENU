using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class FlightPage : IMenuPage
{
    private bool _flightEnabled;

    public string Title => "飞行";

    public void Draw(MenuStyles styles)
    {
        GUILayout.Label(
            "控制角色飞行功能。",
            styles.MutedLabel
        );

        GUILayout.Space(12f);

        bool newValue = GUILayout.Toggle(
            _flightEnabled,
            "启用飞行",
            styles.Toggle,
            GUILayout.Height(40f)
        );

        if (newValue == _flightEnabled)
        {
            return;
        }

        _flightEnabled = newValue;

        Plugin.Log.LogInfo(
            $"Flight enabled: {_flightEnabled}"
        );
    }
}