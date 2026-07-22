using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using P.E.A.K_MENU.UI;

namespace P.E.A.K_MENU.Patches;

/// <summary>
/// 菜单打开期间，阻止 PEAK 更新角色输入。
/// </summary>
[HarmonyPatch]
internal static class CharacterInputPatch
{
    private static MethodInfo? _resetInputMethod;

    /// <summary>
    /// 动态寻找 PEAK 的 CharacterInput 输入采样方法。
    /// 使用字符串查找可以避免因方法访问级别变化而无法编译。
    /// </summary>
    private static IEnumerable<MethodBase> TargetMethods()
    {
        System.Type? characterInputType =
            AccessTools.TypeByName("CharacterInput");

        if (characterInputType is null)
        {
            Plugin.Log.LogError(
                "CharacterInput type was not found. " +
                "Game input blocking will not work."
            );

            yield break;
        }

        _resetInputMethod =
            AccessTools.Method(characterInputType, "ResetInput");

        MethodInfo? sampleMethod =
            AccessTools.Method(characterInputType, "Sample");

        if (sampleMethod is not null)
        {
            Plugin.Log.LogInfo(
                "Patching CharacterInput.Sample."
            );

            yield return sampleMethod;
        }
        else
        {
            Plugin.Log.LogWarning(
                "CharacterInput.Sample was not found."
            );
        }

        MethodInfo? sampleAlwaysMethod =
            AccessTools.Method(characterInputType, "SampleAlways");

        if (sampleAlwaysMethod is not null)
        {
            Plugin.Log.LogInfo(
                "Patching CharacterInput.SampleAlways."
            );

            yield return sampleAlwaysMethod;
        }
        else
        {
            Plugin.Log.LogWarning(
                "CharacterInput.SampleAlways was not found."
            );
        }
    }

    /// <summary>
    /// 返回 false 时，Harmony 会跳过 PEAK 原来的输入采样方法。
    /// </summary>
    private static bool Prefix(object __instance)
    {
        if (!MenuState.IsOpen)
        {
            return true;
        }

        ClearCachedInput(__instance);

        return false;
    }

    private static void ClearCachedInput(object instance)
    {
        if (_resetInputMethod is null)
        {
            return;
        }

        try
        {
            _resetInputMethod.Invoke(instance, null);
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogError(
                $"Failed to reset CharacterInput: {exception}"
            );
        }
    }
}