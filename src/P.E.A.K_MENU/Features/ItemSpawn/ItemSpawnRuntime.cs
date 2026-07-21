using BepInEx.Configuration;

namespace P.E.A.K_MENU.Features.ItemSpawn;

internal static class ItemSpawnRuntime
{
    private static ItemCatalogService? _catalog;
    private static ItemSpawnService? _spawner;

    private static bool _initialized;

    internal static bool IsInitialized =>
        _initialized &&
        _catalog is not null &&
        _spawner is not null;

    internal static ItemCatalogService Catalog
    {
        get
        {
            if (_catalog is null)
            {
                throw new System.InvalidOperationException(
                    "ItemSpawn Catalog 尚未初始化。"
                );
            }

            return _catalog;
        }
    }

    internal static ItemSpawnService Spawner
    {
        get
        {
            if (_spawner is null)
            {
                throw new System.InvalidOperationException(
                    "ItemSpawn Spawner 尚未初始化。"
                );
            }

            return _spawner;
        }
    }

    internal static void Initialize(
        ConfigFile config)
    {
        if (IsInitialized)
        {
            Plugin.Log.LogInfo(
                "ItemSpawn runtime is already initialized."
            );

            return;
        }

        Plugin.Log.LogInfo(
            "Initializing ItemSpawn runtime..."
        );

        ItemSpawnConfiguration.Initialize(config);

        _catalog = new ItemCatalogService();
        _spawner = new ItemSpawnService();

        _initialized = true;

        Plugin.Log.LogInfo(
            "ItemSpawn runtime initialized successfully."
        );
    }

    internal static void Dispose()
    {
        if (!_initialized &&
            _catalog is null &&
            _spawner is null)
        {
            return;
        }

        _catalog?.Dispose();

        _catalog = null;
        _spawner = null;
        _initialized = false;

        Plugin.Log.LogInfo(
            "ItemSpawn runtime disposed."
        );
    }
}