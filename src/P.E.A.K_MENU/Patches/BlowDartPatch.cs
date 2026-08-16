using HarmonyLib;
using P.E.A.K_MENU.Features.BlowDart;
using UnityEngine;

namespace P.E.A.K_MENU.Patches;

[HarmonyPatch]
internal static class BlowDartPatch
{
    private static System.Reflection.MethodBase
        TargetMethod()
    {
        return AccessTools.Method(
            typeof(Action_RaycastDart),
            "DartImpact",
            new[]
            {
                typeof(Character),
                typeof(Vector3),
                typeof(Vector3)
            }
        );
    }

    [HarmonyPrefix]
    private static bool Prefix(
        Action_RaycastDart __instance,
        Character hitCharacter,
        Vector3 origin,
        Vector3 endpoint)
    {
        if (!BlowDartRuntime.IsInitialized ||
            hitCharacter is null)
        {
            return true;
        }

        if (BlowDartRuntime.Service.EffectType ==
            BlowDartEffectType.None)
        {
            /*
             * “无效果”表示不启用自定义替换，
             * 完整保留游戏原版吹箭效果和命中特效。
             */
            return true;
        }

        return !BlowDartRuntime
            .Service
            .TryHandleHit(
                __instance,
                hitCharacter,
                origin,
                endpoint
            );
    }
}
