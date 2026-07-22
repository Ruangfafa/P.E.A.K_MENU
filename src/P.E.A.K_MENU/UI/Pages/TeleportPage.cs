using System.Collections.Generic;
using P.E.A.K_MENU.Features.Teleport;
using UnityEngine;

namespace P.E.A.K_MENU.UI.Pages;

internal sealed class TeleportPage :
    IMenuPage
{
    private const float PlayerRowHeight =
        46f;

    private const float ActionButtonWidth =
        58f;

    private Vector2 _playerScroll;

    public string Title => "传送";

    public void Draw(
        MenuStyles styles)
    {
        if (!TeleportRuntime.IsInitialized)
        {
            GUILayout.Label(
                "传送功能尚未初始化。",
                styles.MutedLabel
            );

            return;
        }

        TeleportService service =
            TeleportRuntime.Service;

        GUILayout.Label(
            "扫描当前房间玩家。点击“去”传送到对方，" +
            "点击“来”将对方传送到自己附近。",
            styles.MutedLabel
        );

        GUILayout.Space(10f);

        DrawPlayerList(
            service,
            styles
        );

        GUILayout.Space(8f);

        GUILayout.Label(
            service.LastStatus,
            service.LastSucceeded
                ? styles.Label
                : styles.MutedLabel
        );

        GUILayout.Space(6f);

        if (GUILayout.Button(
                "刷新房间玩家",
                styles.ActionButton,
                GUILayout.Height(38f)))
        {
            service.RefreshPlayers();
            _playerScroll = Vector2.zero;
        }
    }

    private void DrawPlayerList(
        TeleportService service,
        MenuStyles styles)
    {
        IReadOnlyList<TeleportPlayerEntry>
            players =
                service.Players;

        _playerScroll =
            GUILayout.BeginScrollView(
                _playerScroll,
                false,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        if (players.Count == 0)
        {
            GUILayout.Label(
                Character.localCharacter is null
                    ? "请先进入关卡，随后会自动扫描玩家。"
                    : "房间内暂未发现其他玩家。",
                styles.MutedLabel
            );

            GUILayout.EndScrollView();
            return;
        }

        /*
         * 建立数组快照，避免自动刷新玩家列表时
         * 修改当前正在绘制的集合。
         */
        TeleportPlayerEntry[] snapshot =
            new TeleportPlayerEntry[
                players.Count
            ];

        for (int index = 0;
             index < players.Count;
             index++)
        {
            snapshot[index] =
                players[index];
        }

        foreach (TeleportPlayerEntry player
                 in snapshot)
        {
            DrawPlayerRow(
                player,
                service,
                styles
            );
        }

        GUILayout.EndScrollView();
    }

    private static void DrawPlayerRow(
        TeleportPlayerEntry player,
        TeleportService service,
        MenuStyles styles)
    {
        GUILayout.BeginHorizontal(
            GUILayout.Height(
                PlayerRowHeight
            )
        );

        GUILayout.Label(
            player.DisplayName,
            styles.Label,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(
                PlayerRowHeight
            )
        );

        if (GUILayout.Button(
                "去",
                styles.ActionButton,
                GUILayout.Width(
                    ActionButtonWidth
                ),
                GUILayout.Height(36f)))
        {
            service.TeleportLocalTo(
                player
            );
        }

        if (GUILayout.Button(
                "来",
                styles.ActionButton,
                GUILayout.Width(
                    ActionButtonWidth
                ),
                GUILayout.Height(36f)))
        {
            service.BringPlayerToLocal(
                player
            );
        }

        GUILayout.EndHorizontal();
    }
}