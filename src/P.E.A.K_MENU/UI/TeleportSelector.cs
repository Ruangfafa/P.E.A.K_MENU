using System;
using P.E.A.K_MENU.Features.Teleport;
using P.E.A.K_MENU.Input;
using UnityEngine;
namespace P.E.A.K_MENU.UI;

internal static class TeleportSelector
{
    private static readonly CursorController Cursor = new();
    private static Vector2 _scroll;
    internal static bool IsHeld => !MenuState.IsOpen && !MenuState.IsRebinding &&
        Enum.TryParse<KeyCode>(FeatureInputSettings.GetBinding(FeatureShortcutAction.TeleportSelector).Value, true, out var key) &&
        key != KeyCode.None && key != KeyCode.Mouse0 && key != KeyCode.Mouse1 && UnityEngine.Input.GetKey(key);
    internal static bool IsOpen { get; private set; }
    internal static void Update(bool allowed)
    {
        if (!allowed || !IsHeld || !TeleportRuntime.IsInitialized || Character.localCharacter == null)
        { Close(); return; }
        if (!IsOpen)
        {
            TeleportRuntime.Service.RefreshPlayers();
            _scroll = Vector2.zero;
            IsOpen = true;
            Cursor.Release();
        }
    }
    internal static void LateUpdate()
    {
        if (IsOpen && IsHeld) Cursor.MaintainReleased();
        else Close();
    }
    internal static void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Cursor.Restore();
    }
    internal static void Draw()
    {
        if (!IsOpen || !IsHeld) return;
        float width = Mathf.Min(420f, Screen.width - 24f);
        float height = Mathf.Min(460f, Screen.height - 24f);
        var area = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        using (new GUILayout.AreaScope(area, GUIContent.none, GUI.skin.box))
        {
            GUILayout.Label("按住传送键 · 移动鼠标选择玩家");
            GUILayout.Label(TeleportNetwork.CanBring ? "左键：传送过去    右键：传送过来" : "左键：传送过去    房主未开放拉人");
            using (var scroll = new GUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                var service = TeleportRuntime.Service;
                var players = new System.Collections.Generic.List<TeleportPlayerEntry>(service.Players);
                if (players.Count == 0) GUILayout.Label("当前没有其他玩家。");
                foreach (var player in players)
                {
                    Rect row = GUILayoutUtility.GetRect(new GUIContent(player.DisplayName), GUI.skin.button,
                        GUILayout.Height(40f), GUILayout.ExpandWidth(true));
                    bool hovered = row.Contains(Event.current.mousePosition);
                    if (Event.current.type == EventType.Repaint)
                        GUI.skin.button.Draw(row, new GUIContent(player.DisplayName), hovered, false, hovered, false);
                    if (hovered && Event.current.type == EventType.MouseDown && Event.current.button <= 1)
                    {
                        int button = Event.current.button;
                        Event.current.Use();
                        if (button == 0) service.TeleportLocalTo(player);
                        else if (TeleportNetwork.CanBring) service.BringPlayerToLocal(player);

                    }
                }
            }
            GUILayout.Label(TeleportRuntime.Service.LastStatus);
        }
    }
}
