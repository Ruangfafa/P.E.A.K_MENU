using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.Features.Teleport;
using P.E.A.K_MENU.Input;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU;

[BepInAutoPlugin]
public partial class Plugin :
    BaseUnityPlugin
{
    internal static ManualLogSource Log
    {
        get;
        private set;
    } = null!;

    private PeakMenuWindow
        _menuWindow = null!;

    private MenuInputController
        _inputController = null!;

    private Harmony
        _harmony = null!;

    private void Awake()
    {
        Log = Logger;

        Log.LogInfo(
            "P.E.A.K_MENU Awake started."
        );

        MenuSettings.Initialize(Config);

        InitializeFeature(
            "ItemSpawn",
            () => ItemSpawnRuntime.Initialize(
                Config
            )
        );

        InitializeFeature(
            "Teleport",
            TeleportRuntime.Initialize
        );

        InitializeFeature(
            "Status",
            StatusRuntime.Initialize
        );

        _menuWindow =
            new PeakMenuWindow();

        _inputController =
            new MenuInputController(
                _menuWindow
            );

        _harmony =
            new Harmony(
                "ruangfafa.peakmenu"
            );

        _harmony.PatchAll();

        Log.LogInfo(
            $"Plugin {Name} is loaded!"
        );
    }

    private void Update()
    {
        _inputController.Update();
        _menuWindow.Update();

        TeleportRuntime.Update();
        StatusRuntime.Update();
    }

    private void OnGUI()
    {
        _menuWindow.Draw();
    }

    private void OnDisable()
    {
        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _menuWindow?.Close();
    }

    private void OnDestroy()
    {
        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _menuWindow?.Dispose();

        ItemSpawnRuntime.Dispose();
        TeleportRuntime.Dispose();
        StatusRuntime.Dispose();

        _harmony?.UnpatchSelf();
    }

    private static void InitializeFeature(
        string name,
        System.Action initializer)
    {
        try
        {
            initializer();

            Log.LogInfo(
                $"{name} initialized."
            );
        }
        catch (System.Exception exception)
        {
            Log.LogError(
                $"{name} initialization failed: " +
                $"{exception}"
            );
        }
    }
}