using System;

namespace P.E.A.K_MENU.Features.BlowDart;

internal static class BlowDartRuntime
{
    private static BlowDartService?
        _service;

    internal static bool IsInitialized =>
        _service is not null;

    internal static BlowDartService Service =>
        _service ??
        throw new InvalidOperationException(
            "BlowDartRuntime has not been initialized."
        );

    internal static void Initialize()
    {
        if (_service is not null)
        {
            return;
        }

        _service =
            new BlowDartService();

        Plugin.Log.LogInfo(
            "Blow dart runtime initialized."
        );
    }

    internal static void Dispose()
    {
        _service?.Dispose();
        _service = null;
    }
}