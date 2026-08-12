using System.Collections.Generic;
using BepInEx.Configuration;

namespace P.E.A.K_MENU.Input;

internal static class FeatureInputSettings
{
    private static readonly Dictionary<
        FeatureShortcutAction,
        ConfigEntry<string>> Bindings = new();

    private static ConfigEntry<bool>?
        _doubleTapFlightEnabled;

    internal static bool DoubleTapFlightEnabled
    {
        get =>
            _doubleTapFlightEnabled?.Value ?? true;
        set
        {
            if (_doubleTapFlightEnabled is not null)
            {
                _doubleTapFlightEnabled.Value = value;
            }
        }
    }

    internal static void Initialize(
        ConfigFile config)
    {
        Bindings.Clear();

        _doubleTapFlightEnabled = config.Bind(
            "Flight.Input",
            "DoubleTapSpaceEnabled",
            true,
            "是否允许双击空格进入或退出实际飞行。"
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleFlightSystem,
            "ToggleFlightSystem",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleActiveFlight,
            "ToggleActiveFlight",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.IncreaseFlightSpeed,
            "IncreaseFlightSpeed",
            FeatureInputBinding.MouseWheelUpValue
        );

        Bind(
            config,
            FeatureShortcutAction.DecreaseFlightSpeed,
            "DecreaseFlightSpeed",
            FeatureInputBinding.MouseWheelDownValue
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleInvincibility,
            "ToggleInvincibility",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleAntiKnockback,
            "ToggleAntiKnockback",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleInfiniteStamina,
            "ToggleInfiniteStamina",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.ReviveSelf,
            "ReviveSelf",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.ToggleWeightOverride,
            "ToggleWeightOverride",
            FeatureInputBinding.NoneValue
        );

        Bind(
            config,
            FeatureShortcutAction.SpawnLastItem,
            "SpawnLastItem",
            FeatureInputBinding.NoneValue
        );
    }

    internal static FeatureInputBinding GetBinding(
        FeatureShortcutAction action)
    {
        return Bindings.TryGetValue(
            action,
            out ConfigEntry<string>? entry)
            ? new FeatureInputBinding(entry.Value)
            : new FeatureInputBinding(
                GetDefaultValue(action)
            );
    }

    internal static void SetBinding(
        FeatureShortcutAction action,
        FeatureInputBinding binding)
    {
        if (Bindings.TryGetValue(
                action,
                out ConfigEntry<string>? entry))
        {
            entry.Value = binding.Value;
        }
    }

    internal static void ResetBinding(
        FeatureShortcutAction action)
    {
        SetBinding(
            action,
            new FeatureInputBinding(
                GetDefaultValue(action)
            )
        );
    }

    internal static bool IsPressed(
        FeatureShortcutAction action)
    {
        return GetBinding(action).IsPressed();
    }

    private static void Bind(
        ConfigFile config,
        FeatureShortcutAction action,
        string key,
        string defaultValue)
    {
        Bindings[action] = config.Bind(
            "FeatureShortcuts",
            key,
            defaultValue,
            "功能快捷键。可填写 Unity KeyCode、MouseWheelUp、MouseWheelDown 或 None。"
        );
    }

    private static string GetDefaultValue(
        FeatureShortcutAction action)
    {
        return action switch
        {
            FeatureShortcutAction.IncreaseFlightSpeed =>
                FeatureInputBinding.MouseWheelUpValue,
            FeatureShortcutAction.DecreaseFlightSpeed =>
                FeatureInputBinding.MouseWheelDownValue,
            _ => FeatureInputBinding.NoneValue
        };
    }
}
