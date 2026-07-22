using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Teleport;

/// <summary>
/// 负责扫描房间玩家和执行网络传送。
/// </summary>
internal sealed class TeleportService
{
    private const float AutomaticRefreshInterval =
        3f;

    private const float HorizontalArrivalOffset =
        1.1f;

    private const float VerticalArrivalOffset =
        0.15f;

    private readonly List<TeleportPlayerEntry>
        _players = new();

    private float _nextRefreshTime;

    internal IReadOnlyList<TeleportPlayerEntry>
        Players => _players;

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

    /// <summary>
    /// 按固定间隔自动刷新玩家列表。
    /// </summary>
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
    /// 立即重新扫描当前场景中的角色。
    /// </summary>
    internal void RefreshPlayers()
    {
        _nextRefreshTime =
            Time.unscaledTime +
            AutomaticRefreshInterval;

        Character? localCharacter =
            Character.localCharacter;

        /*
         * 使用 Resources.FindObjectsOfTypeAll，
         * 因为部分网络角色在扫描瞬间可能尚未处于
         *普通 FindObjectsOfType 能找到的状态。
         */
        Character[] discoveredCharacters =
            Resources.FindObjectsOfTypeAll<Character>();

        var discoveredPlayers =
            new List<TeleportPlayerEntry>();

        foreach (Character character
                 in discoveredCharacters)
        {
            if (!IsUsableCharacter(character))
            {
                continue;
            }

            /*
             * 不在列表中显示本地玩家自己。
             */
            if (localCharacter != null &&
                character == localCharacter)
            {
                continue;
            }

            PhotonView? photonView =
                character.photonView;

            if (photonView == null)
            {
                continue;
            }

            int actorNumber =
                ResolveActorNumber(
                    photonView,
                    character
                );

            /*
             * 使用 ActorNumber 去重。
             * 某些场景切换期间可能短暂保留旧角色。
             */
            bool alreadyAdded =
                discoveredPlayers.Any(
                    player =>
                        player.ActorNumber ==
                        actorNumber
                );

            if (alreadyAdded)
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
        _players.AddRange(discoveredPlayers);

        if (Character.localCharacter is null)
        {
            LastSucceeded = false;
            LastStatus =
                "尚未找到本地玩家，请先进入关卡。";

            return;
        }

        if (_players.Count == 0)
        {
            LastSucceeded = false;
            LastStatus =
                "当前没有扫描到其他玩家。";

            return;
        }

        /*
         * 玩家列表刷新成功时，不覆盖刚刚发生的
         * 传送成功提示。
         */
        if (!LastSucceeded ||
            LastStatus.StartsWith(
                "当前没有",
                StringComparison.Ordinal))
        {
            LastSucceeded = true;
            LastStatus =
                $"已扫描到 {_players.Count} 名其他玩家。";
        }
    }

    /// <summary>
    /// 把本地玩家传送到目标玩家附近。
    /// </summary>
    internal void TeleportLocalTo(
        TeleportPlayerEntry target)
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

        if (!ValidateTarget(target))
        {
            return;
        }

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
    /// </summary>
    internal void BringPlayerToLocal(
        TeleportPlayerEntry target)
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

        if (!ValidateTarget(target))
        {
            return;
        }

        Vector3 localPosition =
            ResolveCharacterWorldPosition(
                localCharacter
            );

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
            $"已将 {target.DisplayName} 传送到你附近。";

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

    private bool ValidateTarget(
        TeleportPlayerEntry target)
    {
        if (target is null ||
            !target.IsValid)
        {
            LastStatus =
                "目标玩家已经离开或角色无效。";

            RefreshPlayers();
            return false;
        }

        if (target.Character ==
            Character.localCharacter)
        {
            LastStatus =
                "不能选择本地玩家自己。";

            return false;
        }

        if (target.Character.photonView
            is null)
        {
            LastStatus =
                "目标玩家没有可用的 PhotonView。";

            return false;
        }

        return true;
    }

    /// <summary>
    /// 调用 Character 上的 WarpPlayerRPC。
    ///
    /// RpcTarget.All 会让当前客户端、房主和其他客户端
    /// 都处理相同的位置变化。
    /// </summary>
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
            /*
             * PEAK 当前使用的 RPC 参数：
             *
             * Vector3 destination
             * bool snapCamera
             *
             * false 表示不额外强制镜头跳转。
             */
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

    /// <summary>
    /// 计算目标玩家旁边的位置，
    /// 避免两个角色完全重叠。
    /// </summary>
    private static Vector3 CalculateArrivalPosition(
        Character anchor,
        Character movingCharacter)
    {
        Vector3 anchorPosition =
            ResolveCharacterWorldPosition(anchor);

        Vector3 sideDirection =
            ResolveCharacterSideDirection(anchor);

        int movingActorNumber =
            ResolveActorNumber(
                movingCharacter.photonView,
                movingCharacter
            );

        float directionSign =
            movingActorNumber % 2 == 0
                ? 1f
                : -1f;

        return anchorPosition +
               sideDirection *
               HorizontalArrivalOffset *
               directionSign +
               Vector3.up *
               VerticalArrivalOffset;
    }
    
    /// <summary>
/// 获取角色身体当前实际所在的世界坐标。
///
/// PEAK 的 Character 根节点可能保留在角色生成点，
/// 因此不能直接使用 character.transform.position。
/// 这里优先读取骨盆、臀部或躯干刚体的位置。
/// </summary>
private static Vector3 ResolveCharacterWorldPosition(
    Character character)
{
    if (character is null)
    {
        return Vector3.zero;
    }

    Rigidbody[] rigidbodies =
        character.GetComponentsInChildren<Rigidbody>(
            true
        );

    if (rigidbodies.Length == 0)
    {
        return character.transform.position;
    }

    /*
     * 优先寻找最能代表角色中心位置的身体部位。
     */
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

            return rigidbody.worldCenterOfMass;
        }
    }

    /*
     * 找不到明确的骨盆或躯干时，
     * 使用所有有效身体刚体的平均位置。
     */
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
        return positionSum / validCount;
    }

    return character.transform.position;
}

/// <summary>
/// 判断刚体是否适合作为角色实际位置参考。
/// </summary>
private static bool IsUsableBodyRigidbody(
    Rigidbody? rigidbody)
{
    if (rigidbody is null)
    {
        return false;
    }

    GameObject rigidbodyObject =
        rigidbody.gameObject;

    if (rigidbodyObject is null ||
        !rigidbodyObject.activeInHierarchy)
    {
        return false;
    }

    Vector3 position =
        rigidbody.worldCenterOfMass;

    if (float.IsNaN(position.x) ||
        float.IsNaN(position.y) ||
        float.IsNaN(position.z))
    {
        return false;
    }

    if (float.IsInfinity(position.x) ||
        float.IsInfinity(position.y) ||
        float.IsInfinity(position.z))
    {
        return false;
    }

    return true;
}

/// <summary>
/// 获取角色当前用于左右偏移的方向。
/// 根节点朝向失效时会尝试使用身体刚体朝向。
/// </summary>
private static Vector3 ResolveCharacterSideDirection(
    Character character)
{
    Vector3 sideDirection =
        character.transform.right;

    Rigidbody[] rigidbodies =
        character.GetComponentsInChildren<Rigidbody>(
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
                StringComparison.OrdinalIgnoreCase
            ) ||
            bodyName.Contains(
                "pelvis",
                StringComparison.OrdinalIgnoreCase
            ) ||
            bodyName.Contains(
                "torso",
                StringComparison.OrdinalIgnoreCase
            ) ||
            bodyName.Contains(
                "chest",
                StringComparison.OrdinalIgnoreCase
            );

        if (!isCentralBody)
        {
            continue;
        }

        sideDirection =
            rigidbody.transform.right;

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

        if (character.photonView is null)
        {
            return false;
        }

        return true;
    }

    private static int ResolveActorNumber(
        PhotonView? photonView,
        Character character)
    {
        if (photonView?.Owner is not null)
        {
            return photonView.Owner.ActorNumber;
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