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

        /*
         * 如果正在显示传送页时其他玩家全部离开，
         * 页面不再提供任何可操作按钮。
         */
        if (!service.HasOtherPlayers)
        {
            GUILayout.Label(
                "当前没有其他玩家。",
                styles.MutedLabel
            );

            GUILayout.Space(8f);

            GUILayout.Label(
                "请等待其他玩家加入，页面会自动刷新。",
                styles.MutedLabel
            );

            GUILayout.Space(10f);

            if (GUILayout.Button(
                    "重新扫描房间玩家",
                    styles.ActionButton,
                    GUILayout.Height(38f)))
            {
                service.RefreshPlayers();
                _playerScroll = Vector2.zero;
            }

            return;
        }

        GUILayout.Label(
            "点击“去”传送到对方，" +
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
            /*
             * TeleportLocalTo 内部会先刷新，
             * 再根据 ViewID 与 ActorNumber 获取最新玩家对象。
             */
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
            /*
             * BringPlayerToLocal 内部会先刷新，
             * 再根据 ViewID 与 ActorNumber 获取最新玩家对象。
             */
            service.BringPlayerToLocal(
                player
            );
        }

        GUILayout.EndHorizontal();
    }
}
