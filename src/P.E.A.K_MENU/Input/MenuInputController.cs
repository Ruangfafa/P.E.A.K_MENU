using P.E.A.K_MENU.UI;
using UnityEngine;

namespace P.E.A.K_MENU.Input;

internal sealed class MenuInputController
{
    private readonly PeakMenuWindow _menuWindow;

    internal MenuInputController(
        PeakMenuWindow menuWindow)
    {
        _menuWindow = menuWindow;
    }

    internal void Update()
    {
        // 正在重新指定快捷键时，
        // 所有按键都交给 SettingsPage 处理。
        if (MenuState.IsRebinding)
        {
            return;
        }

        if (_menuWindow.IsOpen)
        {
            if (ShouldCloseMenu())
            {
                _menuWindow.Close();
            }

            return;
        }

        if (UnityEngine.Input.GetKeyDown(
                MenuSettings.ToggleKey))
        {
            _menuWindow.Open();
        }
    }

    private static bool ShouldCloseMenu()
    {
        return
            UnityEngine.Input.GetKeyDown(
                MenuSettings.ToggleKey) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.Escape) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.Tab) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.W) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.A) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.S) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.D) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.LeftShift) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.RightShift) ||
            UnityEngine.Input.GetKeyDown(
                KeyCode.Space);
    }
}