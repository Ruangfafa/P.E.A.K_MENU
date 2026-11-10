using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using P.E.A.K_MENU.Features.ItemSpawn;
using P.E.A.K_MENU.UI;
using UnityEngine;
namespace P.E.A.K_MENU.Input;
internal static class ItemShortcuts
{
    private static ConfigFile _config = null!;
    private static readonly Dictionary<string, ConfigEntry<string>> Entries = new();
    private static string? _waiting;
    internal static void Initialize(ConfigFile config) { _config = config; Entries.Clear(); _waiting = null; }
    private static ConfigEntry<string> Entry(string name)
    {
        if (!Entries.TryGetValue(name, out var entry))
        {
            string key = BitConverter.ToString(Encoding.UTF8.GetBytes(name)).Replace("-", "");
            Entries[name] = entry = _config.Bind("ItemShortcuts", key, "None", "物品快捷键：" + name);
        }
        return entry;
    }
    internal static void Draw(Rect rect, ItemSpawnEntry item, MenuStyles styles)
    {
        var entry = Entry(item.PrefabName);
        var ev = Event.current;
        if (rect.Contains(ev.mousePosition) && ev.type == EventType.MouseDown && ev.button == 1) { entry.Value = "None"; _waiting = null; MenuState.IsRebinding = false; ev.Use(); }
        if (_waiting == item.PrefabName && MenuState.IsRebinding)
        {
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            { _waiting = null; MenuState.IsRebinding = false; ev.Use(); }
            else if (FeatureInputBinding.TryCapture(ev, out var binding))
            { entry.Value = binding.Value; _waiting = null; MenuState.IsRebinding = false; ev.Use(); }
        }
        if (GUI.Button(rect, _waiting == item.PrefabName && MenuState.IsRebinding ? "请按键…" : new FeatureInputBinding(entry.Value).DisplayName, styles.ActionButton))
        { _waiting = item.PrefabName; MenuState.IsRebinding = true; }
    }
    internal static void Update()
    {
        if (!ItemSpawnRuntime.IsInitialized) return;
        foreach (var item in ItemSpawnRuntime.Catalog.AllItems)
            if (new FeatureInputBinding(Entry(item.PrefabName).Value).IsPressed()) ItemSpawnRuntime.Spawner.Spawn(item);
    }
}
