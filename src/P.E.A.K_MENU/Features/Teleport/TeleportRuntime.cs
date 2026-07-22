namespace P.E.A.K_MENU.Features.Teleport;

/// <summary>
/// Teleport 功能的全局运行入口。
/// </summary>
internal static class TeleportRuntime
{
    private static TeleportService?
        _service;

    internal static bool IsInitialized =>
        _service is not null;

    internal static TeleportService Service
    {
        get
        {
            if (_service is null)
            {
                throw new System
                    .InvalidOperationException(
                        "TeleportRuntime 尚未初始化。"
                    );
            }

            return _service;
        }
    }

    internal static void Initialize()
    {
        if (_service is not null)
        {
            return;
        }

        _service =
            new TeleportService();

        Plugin.Log.LogInfo(
            "Teleport runtime initialized."
        );
    }

    internal static void Update()
    {
        _service?.Update();
    }

    internal static void Dispose()
    {
        _service?.Clear();
        _service = null;

        Plugin.Log.LogInfo(
            "Teleport runtime disposed."
        );
    }
}