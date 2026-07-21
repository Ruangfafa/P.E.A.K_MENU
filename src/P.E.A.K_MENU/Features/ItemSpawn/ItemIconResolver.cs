using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace P.E.A.K_MENU.Features.ItemSpawn;

/// <summary>
/// 从 PEAK 的物品数据或预制体中提取物品图标。
/// </summary>
internal sealed class ItemIconResolver : IDisposable
{
    private readonly Dictionary<string, Sprite?>
        _cache =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly List<Sprite>
        _generatedSprites = new();

    internal Sprite? Resolve(
        Item item)
    {
        string key =
            item.name ?? string.Empty;

        if (_cache.TryGetValue(
                key,
                out Sprite? cached))
        {
            return cached;
        }

        Sprite? icon = null;

        try
        {
            /*
             * 优先读取物品自身 UIData 中的图标。
             *
             * PEAK 的 UIData 图标一般是 Texture2D，
             * 因此需要临时转换成 Sprite 供 IMGUI 使用。
             */
            Item.ItemUIData uiData =
                item.UIData;

            icon = CreateSprite(
                uiData.icon
            );

            if (icon is null &&
                uiData.hasAltIcon)
            {
                icon = CreateSprite(
                    uiData.altIcon
                );
            }

            /*
             * 某些物品可能没有标准 UIData 图标，
             * 则继续从预制体子对象查找 Image。
             */
            if (icon is null)
            {
                Image? image =
                    item.GetComponentInChildren<Image>(
                        true
                    );

                icon = image?.sprite;
            }

            /*
             * 最后尝试 SpriteRenderer。
             */
            if (icon is null)
            {
                SpriteRenderer? renderer =
                    item.GetComponentInChildren
                        <SpriteRenderer>(
                            true
                        );

                icon = renderer?.sprite;
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning(
                $"Failed to resolve icon for " +
                $"'{key}': {exception.Message}"
            );
        }

        _cache[key] = icon;

        return icon;
    }

    public void Dispose()
    {
        foreach (Sprite sprite
                 in _generatedSprites)
        {
            if (sprite != null)
            {
                UnityEngine.Object.Destroy(
                    sprite
                );
            }
        }

        _generatedSprites.Clear();
        _cache.Clear();
    }

    private Sprite? CreateSprite(
        Texture2D? texture)
    {
        if (texture is null ||
            texture.width <= 0 ||
            texture.height <= 0)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0f,
                0f,
                texture.width,
                texture.height
            ),
            new Vector2(
                0.5f,
                0.5f
            ),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        sprite.name =
            $"{texture.name}_" +
            $"P.E.A.K_MENU_Icon";

        _generatedSprites.Add(sprite);

        return sprite;
    }
}