using P.E.A.K_MENU.Constants;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU.Input;

internal sealed class MenuInputController
{
    private readonly PeakMenuWindow _menuWindow;

    internal MenuInputController(PeakMenuWindow menuWindow)
    {
        _menuWindow = menuWindow;
    }

    internal void Update()
    {
        if (UnityEngine.Input.GetKeyDown(ModConstants.ToggleMenuKey))
        {
            _menuWindow.Toggle();
        }
    }
}