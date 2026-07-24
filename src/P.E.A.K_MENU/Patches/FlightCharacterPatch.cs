using HarmonyLib;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Flight;

[HarmonyPatch(
    typeof(Character),
    nameof(Character.Awake)
)]
internal static class FlightCharacterPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        Character __instance)
    {
        if (__instance is null ||
            !__instance.IsLocal)
        {
            return;
        }

        FlightController? existing =
            __instance.GetComponent<
                FlightController>();

        if (existing is not null)
        {
            return;
        }

        __instance.gameObject
            .AddComponent<
                FlightController>();

        Plugin.Log.LogInfo(
            $"FlightController added to " +
            $"{__instance.name}."
        );
    }
}