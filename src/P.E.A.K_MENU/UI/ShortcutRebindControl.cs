using P.E.A.K_MENU.Input;
using UnityEngine;

namespace P.E.A.K_MENU.UI;

internal sealed class ShortcutRebindControl
{
    private FeatureShortcutAction?
        _waitingForAction;

    internal void DrawButtons(
        FeatureShortcutAction action,
        MenuStyles styles)
    {
        FeatureInputBinding binding =
            FeatureInputSettings.GetBinding(action);

        string bindingText =
            _waitingForAction == action
                ? "请按按键或滚轮..."
                : $"{binding.DisplayName}（点击修改）";

        if (GUILayout.Button(
                bindingText,
                styles.ActionButton,
                GUILayout.Width(180f),
                GUILayout.Height(38f)))
        {
            _waitingForAction = action;
            MenuState.IsRebinding = true;
        }

        if (GUILayout.Button(
                "恢复默认",
                styles.ActionButton,
                GUILayout.Width(82f),
                GUILayout.Height(38f)))
        {
            Cancel();
            FeatureInputSettings.ResetBinding(action);
        }
    }

    internal void CaptureEvent()
    {
        if (_waitingForAction is null)
        {
            return;
        }

        Event currentEvent = Event.current;

        if (currentEvent is null)
        {
            return;
        }

        if (currentEvent.type == EventType.KeyDown &&
            currentEvent.keyCode == KeyCode.Escape)
        {
            Cancel();
            currentEvent.Use();
            return;
        }

        if (!FeatureInputBinding.TryCapture(
                currentEvent,
                out FeatureInputBinding binding))
        {
            return;
        }

        FeatureInputSettings.SetBinding(
            _waitingForAction.Value,
            binding
        );

        Cancel();
        currentEvent.Use();
    }

    private void Cancel()
    {
        _waitingForAction = null;
        MenuState.IsRebinding = false;
    }
}
