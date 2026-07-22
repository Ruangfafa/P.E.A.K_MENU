using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Teleport;

/// <summary>
/// 扫描房间玩家并执行传送。
/// </summary>
internal sealed class TeleportService
{
    private const float AutomaticRefreshInterval =
        1f;

    private const float HorizontalArrivalOffset =
        1.1f;

    private const float VerticalArrivalOffset =
        0.15f;

    private readonly List<TeleportPlayerEntry>
        _players = new();

    private float _nextRefreshTime;

    internal IReadOnlyList<TeleportPlayerEntry>
        Players => _players;

    internal bool HasOtherPlayers =>
        _players.Count > 0;

    internal string LastStatus
    {
        get;
        private set;
    } = "正在扫描房间玩家……";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void Update()
    {
        if (Time.unscaledTime <
            _nextRefreshTime)
        {
            return;
        }

        RefreshPlayers();

        _nextRefreshTime =
            Time.unscaledTime +
            AutomaticRefreshInterval;
    }

    /// <summary>
    /// 刷新当前房间玩家。
    ///
    /// 返回 true 表示至少存在一名自己以外的玩家。
    /// </summary>
    internal bool RefreshPlayers()
    {
        _nextRefreshTime =
            Time.unscaledTime +
            AutomaticRefreshInterval;

        Character? localCharacter =
            Character.localCharacter;

        Character[] discoveredCharacters =
            Resources.FindObjectsOfTypeAll<
                Character>();

        var discoveredPlayers =
            new List<TeleportPlayerEntry>();

        foreach (Character character
                 in discoveredCharacters)
        {
            if (!IsUsableCharacter(
                    character))
            {
                continue;
            }

            if (localCharacter is not null &&
                character == localCharacter)
            {
                continue;
            }

            PhotonView? photonView =
                character.photonView;

            if (photonView is null)
            {
                continue;
            }

            int actorNumber =
                ResolveActorNumber(
                    photonView,
                    character
                );

            /*
             * ActorNumber 相同的角色只保留一个。
             * 场景切换时可能会短暂残留旧 Character。
             */
            if (discoveredPlayers.Any(
                    player =>
                        player.ActorNumber ==
                        actorNumber))
            {
                continue;
            }

            string displayName =
                ResolveDisplayName(
                    photonView,
                    character,
                    actorNumber
                );

            discoveredPlayers.Add(
                new TeleportPlayerEntry(
                    character,
                    actorNumber,
                    displayName
                )
            );
        }

        discoveredPlayers.Sort(
            (left, right) =>
                string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison
                        .OrdinalIgnoreCase
                )
        );

        _players.Clear();
        _players.AddRange(
            discoveredPlayers
        );

        if (localCharacter is null)
        {
            LastSucceeded = false;
            LastStatus =
                "尚未找到本地玩家，请先进入关卡。";

            return false;
        }

        if (_players.Count == 0)
        {
            LastSucceeded = false;
            LastStatus =
                "当前没有自己以外的玩家。";

            return false;
        }

        LastSucceeded = true;
        LastStatus =
            $"已扫描到 {_players.Count} 名其他玩家。";

        return true;
    }

    /// <summary>
    /// 用于进入传送菜单前检查。
    /// 每次都会立即刷新。
    /// </summary>
    internal bool CanOpenMenu()
    {
        bool available =
            RefreshPlayers();

        if (!available)
        {
            LastSucceeded = false;
            LastStatus =
                "当前没有其他玩家，无法进入传送菜单。";
        }

        return available;
    }

    /// <summary>
    /// 把本地玩家传送到目标玩家附近。
    ///
    /// 执行前重新扫描，并根据 ActorNumber
    /// 获取最新的 Character 对象。
    /// </summary>
    internal void TeleportLocalTo(
        TeleportPlayerEntry selectedTarget)
    {
        LastSucceeded = false;

        Character? localCharacter =
            Character.localCharacter;

        if (localCharacter is null)
        {
            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (!TryResolveFreshTarget(
                selectedTarget,
                out TeleportPlayerEntry?
                    freshTarget))
        {
            return;
        }

        TeleportPlayerEntry target =
            freshTarget!;

        Vector3 targetPosition =
            ResolveCharacterWorldPosition(
                target.Character
            );

        Vector3 destination =
            CalculateArrivalPosition(
                target.Character,
                localCharacter
            );

        Plugin.Log.LogInfo(
            $"TeleportLocalTo target root=" +
            $"{target.Character.transform.position}, " +
            $"body={targetPosition}, " +
            $"destination={destination}."
        );

        if (!TryWarpCharacter(
                localCharacter,
                destination,
                out string error))
        {
            LastStatus =
                $"传送失败：{error}";

            return;
        }

        LastSucceeded = true;
        LastStatus =
            $"已传送到 {target.DisplayName} 附近。";

        Plugin.Log.LogInfo(
            $"Teleported local player to " +
            $"{target.DisplayName} " +
            $"({target.ActorNumber})."
        );
    }

    /// <summary>
    /// 把目标玩家传送到本地玩家附近。
    ///
    /// 执行前重新扫描，并根据 ActorNumber
    /// 获取最新的 Character 对象。
    /// </summary>
    internal void BringPlayerToLocal(
        TeleportPlayerEntry selectedTarget)
    {
        LastSucceeded = false;

        Character? localCharacter =
            Character.localCharacter;

        if (localCharacter is null)
        {
            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (!TryResolveFreshTarget(
                selectedTarget,
                out TeleportPlayerEntry?
                    freshTarget))
        {
            return;
        }

        Vector3 localPosition =
            ResolveCharacterWorldPosition(
                localCharacter
            );

        TeleportPlayerEntry target =
            freshTarget!;

        Vector3 destination =
            CalculateArrivalPosition(
                localCharacter,
                target.Character
            );

        Plugin.Log.LogInfo(
            $"BringPlayerToLocal local root=" +
            $"{localCharacter.transform.position}, " +
            $"body={localPosition}, " +
            $"destination={destination}."
        );

        if (!TryWarpCharacter(
                target.Character,
                destination,
                out string error))
        {
            LastStatus =
                $"传送失败：{error}";

            return;
        }

        LastSucceeded = true;
        LastStatus =
            $"已将 {target.DisplayName} " +
            $"传送到你附近。";

        Plugin.Log.LogInfo(
            $"Brought player " +
            $"{target.DisplayName} " +
            $"({target.ActorNumber}) " +
            $"to the local player."
        );
    }

    internal void Clear()
    {
        _players.Clear();
        _nextRefreshTime = 0f;

        LastSucceeded = false;
        LastStatus =
            "正在扫描房间玩家……";
    }

    /// <summary>
    /// 操作前重新扫描玩家，
    /// 并使用 ActorNumber 获取最新目标。
    /// </summary>
    private bool TryResolveFreshTarget(
        TeleportPlayerEntry selectedTarget,
        out TeleportPlayerEntry? freshTarget)
    {
        freshTarget = null;

        if (selectedTarget is null)
        {
            LastSucceeded = false;
            LastStatus =
                "目标玩家无效。";

            return false;
        }

        int actorNumber =
            selectedTarget.ActorNumber;

        /*
         * 每次点击“去”或“来”之前强制刷新。
         */
        if (!RefreshPlayers())
        {
            LastSucceeded = false;
            LastStatus =
                "房间中已没有其他玩家。";

            return false;
        }

        freshTarget =
            _players.FirstOrDefault(
                player =>
                    player.ActorNumber ==
                    actorNumber
            );

        if (freshTarget is null)
        {
            LastSucceeded = false;
            LastStatus =
                "目标玩家已经离开房间。";

            return false;
        }

        if (!freshTarget.IsValid)
        {
            LastSucceeded = false;
            LastStatus =
                "目标玩家角色已经失效。";

            RefreshPlayers();

            freshTarget = null;
            return false;
        }

        if (freshTarget.Character ==
            Character.localCharacter)
        {
            LastSucceeded = false;
            LastStatus =
                "不能选择本地玩家自己。";

            freshTarget = null;
            return false;
        }

        if (freshTarget.Character.photonView
            is null)
        {
            LastSucceeded = false;
            LastStatus =
                "目标玩家没有可用的 PhotonView。";

            freshTarget = null;
            return false;
        }

        return true;
    }

    private static bool TryWarpCharacter(
        Character character,
        Vector3 destination,
        out string error)
    {
        error = string.Empty;

        if (character is null)
        {
            error = "角色对象为空。";
            return false;
        }

        PhotonView? photonView =
            character.photonView;

        if (photonView is null)
        {
            error = "角色 PhotonView 不存在。";
            return false;
        }

        try
        {
            photonView.RPC(
                "WarpPlayerRPC",
                RpcTarget.All,
                destination,
                false
            );

            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;

            Plugin.Log.LogError(
                $"WarpPlayerRPC failed for " +
                $"'{character.name}': " +
                $"{exception}"
            );

            return false;
        }
    }

    private static Vector3
        CalculateArrivalPosition(
            Character anchor,
            Character movingCharacter)
    {
        Vector3 anchorPosition =
            ResolveCharacterWorldPosition(
                anchor
            );

        Vector3 sideDirection =
            ResolveCharacterSideDirection(
                anchor
            );

        int movingActorNumber =
            ResolveActorNumber(
                movingCharacter.photonView,
                movingCharacter
            );

        float directionSign =
            movingActorNumber % 2 == 0
                ? 1f
                : -1f;

        return
            anchorPosition +
            sideDirection *
            HorizontalArrivalOffset *
            directionSign +
            Vector3.up *
            VerticalArrivalOffset;
    }

    private static Vector3
        ResolveCharacterWorldPosition(
            Character character)
    {
        Rigidbody[] rigidbodies =
            character.GetComponentsInChildren<
                Rigidbody>(
                    true
                );

        if (rigidbodies.Length == 0)
        {
            return character
                .transform
                .position;
        }

        string[] preferredBodyNames =
        {
            "hip",
            "hips",
            "pelvis",
            "torso",
            "chest",
            "body"
        };

        foreach (string preferredName
                 in preferredBodyNames)
        {
            foreach (Rigidbody rigidbody
                     in rigidbodies)
            {
                if (!IsUsableBodyRigidbody(
                        rigidbody))
                {
                    continue;
                }

                string rigidbodyName =
                    rigidbody.name ??
                    string.Empty;

                if (!rigidbodyName.Contains(
                        preferredName,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    continue;
                }

                return rigidbody
                    .worldCenterOfMass;
            }
        }

        Vector3 positionSum =
            Vector3.zero;

        int validCount = 0;

        foreach (Rigidbody rigidbody
                 in rigidbodies)
        {
            if (!IsUsableBodyRigidbody(
                    rigidbody))
            {
                continue;
            }

            positionSum +=
                rigidbody.worldCenterOfMass;

            validCount++;
        }

        if (validCount > 0)
        {
            return
                positionSum /
                validCount;
        }

        return character
            .transform
            .position;
    }

    private static bool
        IsUsableBodyRigidbody(
            Rigidbody? rigidbody)
    {
        if (rigidbody is null)
        {
            return false;
        }

        GameObject rigidbodyObject =
            rigidbody.gameObject;

        if (rigidbodyObject is null ||
            !rigidbodyObject
                .activeInHierarchy)
        {
            return false;
        }

        Vector3 position =
            rigidbody.worldCenterOfMass;

        return
            !float.IsNaN(position.x) &&
            !float.IsNaN(position.y) &&
            !float.IsNaN(position.z) &&
            !float.IsInfinity(position.x) &&
            !float.IsInfinity(position.y) &&
            !float.IsInfinity(position.z);
    }

    private static Vector3
        ResolveCharacterSideDirection(
            Character character)
    {
        Vector3 sideDirection =
            character.transform.right;

        Rigidbody[] rigidbodies =
            character.GetComponentsInChildren<
                Rigidbody>(
                    true
                );

        foreach (Rigidbody rigidbody
                 in rigidbodies)
        {
            if (!IsUsableBodyRigidbody(
                    rigidbody))
            {
                continue;
            }

            string bodyName =
                rigidbody.name ??
                string.Empty;

            bool isCentralBody =
                bodyName.Contains(
                    "hip",
                    StringComparison
                        .OrdinalIgnoreCase
                ) ||
                bodyName.Contains(
                    "pelvis",
                    StringComparison
                        .OrdinalIgnoreCase
                ) ||
                bodyName.Contains(
                    "torso",
                    StringComparison
                        .OrdinalIgnoreCase
                ) ||
                bodyName.Contains(
                    "chest",
                    StringComparison
                        .OrdinalIgnoreCase
                );

            if (!isCentralBody)
            {
                continue;
            }

            sideDirection =
                rigidbody
                    .transform
                    .right;

            break;
        }

        sideDirection.y = 0f;

        if (sideDirection.sqrMagnitude <
            0.001f)
        {
            return Vector3.right;
        }

        return sideDirection.normalized;
    }

    private static bool IsUsableCharacter(
        Character? character)
    {
        if (character is null)
        {
            return false;
        }

        GameObject gameObject =
            character.gameObject;

        if (gameObject is null ||
            !gameObject.scene.IsValid())
        {
            return false;
        }

        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        return character.photonView
               is not null;
    }

    private static int ResolveActorNumber(
        PhotonView? photonView,
        Character character)
    {
        if (photonView?.Owner is not null)
        {
            return photonView
                .Owner
                .ActorNumber;
        }

        if (photonView is not null &&
            photonView.ViewID != 0)
        {
            return photonView.ViewID;
        }

        return character.GetInstanceID();
    }

    private static string ResolveDisplayName(
        PhotonView photonView,
        Character character,
        int actorNumber)
    {
        string? nickname =
            photonView.Owner?.NickName;

        if (!string.IsNullOrWhiteSpace(
                nickname))
        {
            return nickname;
        }

        if (!string.IsNullOrWhiteSpace(
                character.name))
        {
            return character.name;
        }

        return $"玩家 {actorNumber}";
    }
}