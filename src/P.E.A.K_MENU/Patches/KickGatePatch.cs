using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using P.E.A.K_MENU.UI.Pages;
namespace P.E.A.K_MENU.Patches;

// Gate the native entry points regardless of which mod invoked them.
[HarmonyPatch]
internal static class KickGatePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CharacterGrabbing), "RPCA_Kick");
        yield return AccessTools.Method(typeof(CharacterGrabbing), "KickCast");
        yield return AccessTools.Method(typeof(CharacterGrabbing), "RPCA_KickImpact");
    }
    [HarmonyPriority(Priority.First)]
    private static bool Prefix() => KickPage.Enabled;
}
