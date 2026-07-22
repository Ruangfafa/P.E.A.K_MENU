namespace P.E.A.K_MENU.Features.Status;

/// <summary>
/// 状态页面中的一个可施加效果。
/// </summary>
internal sealed class StatusEffectDefinition
{
    internal StatusEffectDefinition(
        string id,
        string displayName,
        string description,
        StatusEffectKind kind,
        StatusValueMode valueMode,
        bool showAmount,
        bool showDuration,
        CharacterAfflictions.STATUSTYPE? statusType = null,
        float defaultAmount = 0.10f,
        float defaultDuration = 3f)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;

        Kind = kind;
        ValueMode = valueMode;

        ShowAmount = showAmount;
        ShowDuration = showDuration;

        StatusType = statusType;

        DefaultAmount = defaultAmount;
        DefaultDuration = defaultDuration;
    }

    internal string Id { get; }

    internal string DisplayName { get; }

    internal string Description { get; }

    internal StatusEffectKind Kind { get; }

    internal StatusValueMode ValueMode { get; }

    internal bool ShowAmount { get; }

    internal bool ShowDuration { get; }

    internal CharacterAfflictions.STATUSTYPE?
        StatusType { get; }

    internal float DefaultAmount { get; }

    internal float DefaultDuration { get; }
}

internal enum StatusEffectKind
{
    GameStatus
}

internal enum StatusValueMode
{
    AmountOnly,
    NativeDecay,
    TimedConstant,
    TimedDecay
}

internal enum StatusApplyMode
{
    Add,
    Subtract,
    Set,
    Clear
}