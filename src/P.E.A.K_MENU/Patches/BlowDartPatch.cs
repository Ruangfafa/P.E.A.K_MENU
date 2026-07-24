using HarmonyLib;
using UnityEngine;

namespace P.E.A.K_MENU.Features.BlowDart;

/// <summary>
/// 在吹箭准备发送原版命中 RPC 前拦截。
///
/// 自定义效果启用时：
///
/// 1. 直接使用 CharacterAfflictions.AddStatus
///    对命中角色施加基础状态；
///
/// 2. 返回 false，阻止原版 DartImpact 继续执行，
///    从而避免原版睡眠效果通过 RPC 发送给队友。
/// </summary>
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
        if (!BlowDartRuntime.IsInitialized)
        {
            return true;
        }

        BlowDartService service =
            BlowDartRuntime.Service;

        if (!service.Enabled ||
            service.EffectType ==
            BlowDartEffectType.Original)
        {
            /*
             * 功能关闭或选择原版时，
             * 继续执行游戏原本的睡眠吹箭。
             */
            return true;
        }

        if (__instance is null)
        {
            return true;
        }

        if (hitCharacter is null)
        {
            /*
             * 没有命中角色时继续执行原版逻辑，
             * 保留击中墙面或环境的表现。
             */
            return true;
        }

        try
        {
            bool applied =
                service.TryApplyDirectEffect(
                    hitCharacter
                );

            if (!applied)
            {
                /*
                 * 应用失败时不拦截原版，
                 * 避免吹箭完全失效。
                 */
                return true;
            }

            Plugin.Log.LogInfo(
                $"Direct blow dart effect applied to " +
                $"{hitCharacter.characterName}; " +
                $"effect={service.EffectType}; " +
                $"amount={service.Amount:0.###}."
            );

            /*
             * 关键：
             *
             * 阻止原版 DartImpact。
             * 否则它仍会发送 RPC_DartImpact，
             * 队友客户端就会继续应用原版睡眠。
             */
            return false;
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogError(
                $"Direct blow dart patch failed: " +
                $"{exception}"
            );

            return true;
        }
    }
}