namespace P.E.A.K_MENU.Features.Status;

internal static class StatusRuntime
{
    private static StatusService? _service;

    internal static bool IsInitialized =>
        _service is not null;

    internal static StatusService Service
    {
        get
        {
            if (_service is null)
            {
                throw new System
                    .InvalidOperationException(
                        "StatusRuntime 尚未初始化。"
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
            new StatusService();

        Plugin.Log.LogInfo(
            "Status runtime initialized."
        );
    }

    internal static void Update()
    {
        _service?.Update();
    }

    internal static void Dispose()
    {
        _service?.Dispose();
        _service = null;

        Plugin.Log.LogInfo(
            "Status runtime disposed."
        );
    }
}