namespace P.E.A.K_MENU.Features.Flight;

internal static class FlightRuntime
{
    private static FlightService? _service;

    internal static bool IsInitialized =>
        _service is not null;

    internal static FlightService Service
    {
        get
        {
            if (_service is null)
            {
                throw new System
                    .InvalidOperationException(
                        "FlightRuntime 尚未初始化。"
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
            new FlightService();

        Plugin.Log.LogInfo(
            "Flight runtime initialized."
        );
    }

    internal static void Update()
    {
        _service?.Update();
    }

    internal static void FixedUpdate()
    {
        _service?.FixedUpdate();
    }

    internal static void Dispose()
    {
        _service?.Dispose();
        _service = null;

        Plugin.Log.LogInfo(
            "Flight runtime disposed."
        );
    }
}