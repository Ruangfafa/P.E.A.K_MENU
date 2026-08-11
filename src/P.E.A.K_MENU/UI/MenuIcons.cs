using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal static class MenuIcons
{
    private static readonly Dictionary<
        string,
        Texture2D> Icons = new();

    internal static Texture2D? Item =>
        Get("item");

    internal static Texture2D? Teleport =>
        Get("teleport");

    internal static Texture2D? Flight =>
        Get("flight");

    internal static Texture2D? Status =>
        Get("status");
    
    internal static void Initialize()
    {
        Dispose();

        Assembly assembly =
            typeof(MenuIcons).Assembly;

        /*
         * 输出 DLL 中实际存在的嵌入资源，
         * 方便检查资源名称是否正确。
         */
        string[] resourceNames =
            assembly.GetManifestResourceNames();

        Plugin.Log.LogInfo(
            $"Embedded resources: " +
            $"{string.Join(", ", resourceNames)}"
        );

        Load(
            assembly,
            "item",
            "item.png"
        );

        Load(
            assembly,
            "teleport",
            "teleport.png"
        );

        Load(
            assembly,
            "flight",
            "flight.png"
        );

        Load(
            assembly,
            "status",
            "status.png"
        );
        
        Plugin.Log.LogInfo(
            $"Loaded {Icons.Count} menu icons."
        );
    }

    internal static void Dispose()
    {
        foreach (Texture2D texture
                 in Icons.Values)
        {
            if (texture is null)
            {
                continue;
            }

            UnityEngine.Object.Destroy(
                texture
            );
        }

        Icons.Clear();
    }

    private static Texture2D? Get(
        string key)
    {
        return Icons.TryGetValue(
            key,
            out Texture2D? texture)
            ? texture
            : null;
    }

    private static void Load(
        Assembly assembly,
        string key,
        string fileName)
    {
        try
        {
            /*
             * 不再写死完整资源名称。
             *
             * 自动搜索以 item.png、
             * teleport.png 等结尾的资源，
             * 避免 RootNamespace 或目录结构变化。
             */
            string? resourceName =
                assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(
                        name =>
                            name.EndsWith(
                                fileName,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                    );

            if (resourceName is null)
            {
                Plugin.Log.LogWarning(
                    $"Menu icon resource not found: " +
                    $"{fileName}"
                );

                return;
            }

            using Stream? stream =
                assembly
                    .GetManifestResourceStream(
                        resourceName
                    );

            if (stream is null)
            {
                Plugin.Log.LogWarning(
                    $"Unable to open menu icon: " +
                    $"{resourceName}"
                );

                return;
            }

            byte[] bytes;

            using (var memoryStream =
                   new MemoryStream())
            {
                stream.CopyTo(
                    memoryStream
                );

                bytes =
                    memoryStream.ToArray();
            }

            var texture =
                new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                )
                {
                    name =
                        $"P.E.A.K_MENU_{key}_Icon",

                    filterMode =
                        FilterMode.Bilinear,

                    wrapMode =
                        TextureWrapMode.Clamp,

                    hideFlags =
                        HideFlags.HideAndDontSave
                };

            bool loaded =
                ImageConversion.LoadImage(
                    texture,
                    bytes,
                    markNonReadable: false
                );

            if (!loaded)
            {
                UnityEngine.Object.Destroy(
                    texture
                );

                Plugin.Log.LogWarning(
                    $"Failed to decode menu icon: " +
                    $"{resourceName}"
                );

                return;
            }

            Icons[key] =
                texture;

            Plugin.Log.LogInfo(
                $"Loaded menu icon '{key}': " +
                $"{resourceName}, " +
                $"{texture.width}x{texture.height}."
            );
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to load menu icon " +
                $"'{fileName}': {exception}"
            );
        }
    }
}
