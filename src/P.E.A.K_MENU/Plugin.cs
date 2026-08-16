using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using P.E.A.K_MENU.Features.Flight;
using P.E.A.K_MENU.Features.BlowDart;
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

    private ChangelogOverlay
        _changelogOverlay = null!;

    private AnnouncementOverlay
        _announcementOverlay = null!;

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
            "BlowDart",
            () => BlowDartRuntime.Initialize(
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

        _changelogOverlay =
            new ChangelogOverlay();

        _announcementOverlay =
            new AnnouncementOverlay();

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
        _changelogOverlay.Update();

        _announcementOverlay.Update(
            canShow:
                !_changelogOverlay.IsVisible
        );

        if (!_changelogOverlay.IsVisible &&
            !_announcementOverlay.IsVisible)
        {
            _inputController.Update();
            _featureShortcutController.Update();
        }

        _menuWindow.Update();

        TeleportRuntime.Update();
        StatusRuntime.Update();
        FlightRuntime.Update();
        BlowDartRuntime.Update();
    }

    private void OnGUI()
    {
        _menuWindow.Draw();
        _changelogOverlay.Draw();
        _announcementOverlay.Draw();
    }

    private void LateUpdate()
    {
        _menuWindow.LateUpdate();
        _changelogOverlay.LateUpdate();
        _announcementOverlay.LateUpdate();
    }

    private void OnDisable()
    {
        _changelogOverlay
            ?.HideWithoutAcknowledging();

        _announcementOverlay
            ?.HideWithoutAcknowledging();

        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _menuWindow?.Close();
    }

    private void OnDestroy()
    {
        MenuState.IsOpen = false;
        MenuState.IsRebinding = false;

        _menuWindow?.Dispose();
        _changelogOverlay?.Dispose();
        _announcementOverlay?.Dispose();
        
        MenuIcons.Dispose();

        /*
         * Flight 必须先于 Status 释放，
         * 才能恢复保存的状态。
         */
        FlightRuntime.Dispose();
        BlowDartRuntime.Dispose();
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
