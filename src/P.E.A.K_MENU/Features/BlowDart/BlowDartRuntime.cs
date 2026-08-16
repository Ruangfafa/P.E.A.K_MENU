using System;
using BepInEx.Configuration;

namespace P.E.A.K_MENU.Features.BlowDart;

internal static class BlowDartRuntime
{
    private static BlowDartService? _service;

    internal static bool IsInitialized =>
        _service is not null;

    internal static BlowDartService Service =>
        _service ??
        throw new InvalidOperationException(
            "BlowDartRuntime 尚未初始化。"
        );

    internal static void Initialize(
        ConfigFile config)
    {
        if (_service is not null)
        {
            return;
        }

        _service =
            new BlowDartService(config);

        Plugin.Log.LogInfo(
            "Blow dart runtime initialized."
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
    }
}
