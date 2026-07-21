using BepInEx;
using BepInEx.Logging;
using P.E.A.K_MENU.Input;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    private PeakMenuWindow _menuWindow = null!;
    private MenuInputController _inputController = null!;

    private void Awake()
    {
        Log = Logger;

        _menuWindow = new PeakMenuWindow();
        _inputController = new MenuInputController(_menuWindow);

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void Update()
    {
        _inputController.Update();
    }

    private void OnGUI()
    {
        _menuWindow.Draw();
    }

    private void OnDisable()
    {
        _menuWindow.Close();
    }
}