using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using P.E.A.K_MENU.Features.Flight;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.Features.Teleport;
using P.E.A.K_MENU.Input;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU;

[BepInAutoPlugin]
[UnityEngine.DefaultExecutionOrder(10000)]
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

    private FeatureShortcutController
        _featureShortcutController = null!;

    private Harmony
        _harmony = null!;

    private void Awake()
    {
        Log = Logger;

        Log.LogInfo(
            "P.E.A.K_MENU Awake started."
        );

        MenuSettings.Initialize(Config);
        FeatureInputSettings.Initialize(Config);
        
        MenuIcons.Initialize();

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

        /*
         * Flight 依赖 Status，
         * 所以必须先初始化 Status。
         */
        InitializeFeature(
            "Status",
            StatusRuntime.Initialize
        );

        InitializeFeature(
            "Flight",
            () => FlightRuntime.Initialize(
                Config
            )
        );

        InitializeFeature(
            "Update defaults",
            () => ModUpdateSettings.Apply(
                Config,
                Info.Metadata.Version.ToString()
            )
        );

        _menuWindow =
            new PeakMenuWindow();

        _inputController =
            new MenuInputController(
                _menuWindow
            );

        _featureShortcutController =
            new FeatureShortcutController();

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
        _featureShortcutController.Update();

        TeleportRuntime.Update();
        StatusRuntime.Update();
        FlightRuntime.Update();
    }

    private void OnGUI()
    {
        _menuWindow.Draw();
    }

    private void LateUpdate()
    {
        _menuWindow.LateUpdate();
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
        
        MenuIcons.Dispose();

        /*
         * Flight 必须先于 Status 释放，
         * 才能恢复保存的状态。
         */
        FlightRuntime.Dispose();
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
