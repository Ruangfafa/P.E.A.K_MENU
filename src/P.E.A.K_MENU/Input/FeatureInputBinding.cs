using System;
using UnityEngine;

namespace P.E.A.K_MENU.Input;

internal readonly struct FeatureInputBinding
{
    internal const string NoneValue = "None";
    internal const string MouseWheelUpValue =
        "MouseWheelUp";
    internal const string MouseWheelDownValue =
        "MouseWheelDown";

    private readonly string _value;

    internal FeatureInputBinding(
        string? value)
    {
        _value = Normalize(value);
    }

    internal string Value =>
        _value ?? NoneValue;

    internal string DisplayName =>
        Value switch
        {
            NoneValue => "未指定",
            MouseWheelUpValue => "滚轮向上",
            MouseWheelDownValue => "滚轮向下",
            _ => Value
        };

    internal bool IsPressed()
    {
        if (Value == MouseWheelUpValue)
        {
            return UnityEngine.Input
                .mouseScrollDelta.y > 0f;
        }

        if (Value == MouseWheelDownValue)
        {
            return UnityEngine.Input
                .mouseScrollDelta.y < 0f;
        }

        return Enum.TryParse(
                   Value,
                   true,
                   out KeyCode keyCode
               ) &&
               keyCode != KeyCode.None &&
               UnityEngine.Input.GetKeyDown(
                   keyCode
               );
    }

    internal static bool TryCapture(
        Event currentEvent,
        out FeatureInputBinding binding)
    {
        if (currentEvent.type ==
            EventType.ScrollWheel)
        {
            binding = new FeatureInputBinding(
                currentEvent.delta.y < 0f
                    ? MouseWheelUpValue
                    : MouseWheelDownValue
            );

            return true;
        }

        if (currentEvent.type ==
                EventType.KeyDown &&
            currentEvent.keyCode != KeyCode.None)
        {
            binding = new FeatureInputBinding(
                currentEvent.keyCode.ToString()
            );

            return true;
        }

        binding = default;
        return false;
    }

    private static string Normalize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return NoneValue;
        }

        string trimmed = value.Trim();

        if (trimmed.Equals(
                MouseWheelUpValue,
                StringComparison.OrdinalIgnoreCase))
        {
            return MouseWheelUpValue;
        }

        if (trimmed.Equals(
                MouseWheelDownValue,
                StringComparison.OrdinalIgnoreCase))
        {
            return MouseWheelDownValue;
        }

        if (Enum.TryParse(
                trimmed,
                true,
                out KeyCode keyCode) &&
            keyCode != KeyCode.None)
        {
            return keyCode.ToString();
        }

        return NoneValue;
    }
}
