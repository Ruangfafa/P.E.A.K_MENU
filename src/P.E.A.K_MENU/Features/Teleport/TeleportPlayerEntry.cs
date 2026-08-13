namespace P.E.A.K_MENU.Features.Teleport;

/// <summary>
/// 表示当前房间内一个可传送的玩家。
/// </summary>
internal sealed class TeleportPlayerEntry
{
    internal TeleportPlayerEntry(
        Character character,
        int actorNumber,
        int viewId,
        string displayName)
    {
        Character = character;
        ActorNumber = actorNumber;
        ViewId = viewId;
        DisplayName = displayName;
    }

    /// <summary>
    /// 对应的游戏角色对象。
    /// </summary>
    internal Character Character { get; }

    /// <summary>
    /// Photon 玩家编号。
    /// 用于去重和识别玩家。
    /// </summary>
    internal int ActorNumber { get; }

    /// <summary>
    /// 当前玩家角色自身的 Photon ViewID。
    /// </summary>
    internal int ViewId { get; }

    /// <summary>
    /// 菜单中显示的玩家名称。
    /// </summary>
    internal string DisplayName { get; }

    internal bool IsValid =>
        Character != null &&
        Character.gameObject != null &&
        Character.gameObject.activeInHierarchy;

    public override string ToString()
    {
        return
            $"{DisplayName} " +
            $"({ActorNumber}, {ViewId})";
    }
}
