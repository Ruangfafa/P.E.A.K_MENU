using P.E.A.K_MENU.Features.Flight;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU.Input;

internal sealed class FeatureShortcutController
{
    internal void Update()
    {
        if (MenuState.IsOpen ||
            MenuState.IsRebinding)
        {
            return;
        }

        UpdateFlightShortcuts();
        UpdateStatusShortcuts();
    }

    private static void UpdateFlightShortcuts()
    {
        if (!FlightRuntime.IsInitialized)
        {
            return;
        }

        FlightService service =
            FlightRuntime.Service;

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleFlightSystem))
        {
            service.SetEnabled(!service.Enabled);
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleActiveFlight))
        {
            service.ToggleActiveFlight();
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .IncreaseFlightSpeed))
        {
            service.AdjustFlightSpeed(1f);
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .DecreaseFlightSpeed))
        {
            service.AdjustFlightSpeed(-1f);
        }
    }

    private static void UpdateStatusShortcuts()
    {
        if (!StatusRuntime.IsInitialized)
        {
            return;
        }

        StatusService service =
            StatusRuntime.Service;

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleInvincibility) &&
            !service.FlightProtectionLock)
        {
            service.SetInvincible(
                !service.Invincible
            );
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleAntiKnockback) &&
            service.Invincible &&
            !service.FlightProtectionLock)
        {
            service.SetAntiKnockback(
                !service.AntiKnockback
            );
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleInfiniteStamina))
        {
            service.SetInfiniteStamina(
                !service.InfiniteStamina
            );
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction.ReviveSelf))
        {
            service.ReviveSelf();
        }

        if (FeatureInputSettings.IsPressed(
                FeatureShortcutAction
                    .ToggleWeightOverride))
        {
            service.SetWeightOverride(
                !service.WeightOverrideEnabled
            );
        }
    }
}
