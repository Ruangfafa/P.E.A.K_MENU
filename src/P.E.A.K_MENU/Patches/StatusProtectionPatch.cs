using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace P.E.A.K_MENU.Patches;

/// <summary>
/// 无敌、防击退和负重覆盖补丁。
/// </summary>
[HarmonyPatch]
internal static class StatusProtectionPatch
{
    internal static bool InvincibleEnabled
    {
        get;
        set;
    }

    internal static bool AntiKnockbackEnabled
    {
        get;
        set;
    } = true;

    internal static bool WeightOverrideEnabled
    {
        get;
        set;
    }

    internal static float WeightOverrideValue
    {
        get;
        set;
    }

    private static IEnumerable<MethodBase>
        TargetMethods()
    {
        string[] methodNames =
        {
            /*
             * 死亡。
             */
            "DieInstantly",
            "RPCA_Die",

            /*
             * 摔倒。
             */
            "Fall",
            "RPCA_Fall",
            "RPCA_FallWithScreenShake",

            /*
             * 外力和击退。
             */
            "AddForce",
            "AddForceAtPosition",
            "RPCA_AddForceAtPosition",
            "AddForceToBodyPart",
            "RPCA_AddForceToBodyPart"
        };

        MethodInfo[] characterMethods =
            AccessTools
                .GetDeclaredMethods(
                    typeof(Character)
                )
                .ToArray();

        foreach (string methodName
                 in methodNames)
        {
            MethodInfo[] matches =
                characterMethods
                    .Where(
                        method =>
                            method.Name.Equals(
                                methodName,
                                StringComparison.Ordinal
                            )
                    )
                    .ToArray();

            foreach (MethodInfo method
                     in matches)
            {
                Plugin.Log.LogInfo(
                    $"Status protection patch target: " +
                    $"Character.{method.Name}(" +
                    $"{string.Join(
                        ", ",
                        method
                            .GetParameters()
                            .Select(
                                parameter =>
                                    parameter
                                        .ParameterType
                                        .Name)
                    )})"
                );

                yield return method;
            }
        }
    }

    private static bool Prefix(
        object __instance,
        MethodBase __originalMethod)
    {
        if (!InvincibleEnabled)
        {
            return true;
        }

        if (__instance is not Character character ||
            !character.IsLocal)
        {
            return true;
        }

        string methodName =
            __originalMethod.Name;

        bool deathMethod =
            methodName == "DieInstantly" ||
            methodName == "RPCA_Die";

        if (deathMethod)
        {
            Plugin.Log.LogInfo(
                $"Blocked local death: {methodName}"
            );

            return false;
        }

        if (!AntiKnockbackEnabled)
        {
            return true;
        }

        bool fallMethod =
            methodName == "Fall" ||
            methodName == "RPCA_Fall" ||
            methodName ==
            "RPCA_FallWithScreenShake";

        if (fallMethod)
        {
            Plugin.Log.LogInfo(
                $"Blocked local fall: {methodName}"
            );

            return false;
        }

        bool forceMethod =
            methodName == "AddForce" ||
            methodName == "AddForceAtPosition" ||
            methodName ==
            "RPCA_AddForceAtPosition" ||
            methodName == "AddForceToBodyPart" ||
            methodName ==
            "RPCA_AddForceToBodyPart";

        if (!forceMethod)
        {
            return true;
        }

        /*
         * 攀爬期间必须允许 Character 的外力方法执行，
         * 否则向上的攀爬推动力也会被当成击退拦截。
         */
        if (IsCharacterClimbing(
                character))
        {
            return true;
        }

        Plugin.Log.LogInfo(
            $"Blocked local force: {methodName}"
        );

        return false;
    }
    
    private static bool IsCharacterClimbing(
    Character character)
{
    if (character is null)
    {
        return false;
    }

    /*
     * 不直接写死字段，避免不同 PEAK 版本
     * 攀爬状态成员名称发生变化。
     */
    string[] memberNames =
    {
        "isClimbing",
        "IsClimbing",
        "climbing",
        "Climbing",
        "isClimb",
        "IsClimb"
    };

    Type characterType =
        character.GetType();

    foreach (string memberName
             in memberNames)
    {
        PropertyInfo? property =
            AccessTools.Property(
                characterType,
                memberName
            );

        if (property is not null &&
            property.PropertyType ==
                typeof(bool))
        {
            try
            {
                object? value =
                    property.GetValue(
                        character
                    );

                if (value is bool result &&
                    result)
                {
                    return true;
                }
            }
            catch
            {
                // 继续检查其他成员。
            }
        }

        FieldInfo? field =
            AccessTools.Field(
                characterType,
                memberName
            );

        if (field is not null &&
            field.FieldType ==
                typeof(bool))
        {
            try
            {
                object? value =
                    field.GetValue(
                        character
                    );

                if (value is bool result &&
                    result)
                {
                    return true;
                }
            }
            catch
            {
                // 继续检查其他成员。
            }
        }
    }

    /*
     * 某些版本会把攀爬状态放在 CharacterData 中。
     */
    object? data =
        character.data;

    if (data is null)
    {
        return false;
    }

    Type dataType =
        data.GetType();

    foreach (string memberName
             in memberNames)
    {
        PropertyInfo? property =
            AccessTools.Property(
                dataType,
                memberName
            );

        if (property is not null &&
            property.PropertyType ==
                typeof(bool))
        {
            try
            {
                object? value =
                    property.GetValue(
                        data
                    );

                if (value is bool result &&
                    result)
                {
                    return true;
                }
            }
            catch
            {
                // 继续检查其他成员。
            }
        }

        FieldInfo? field =
            AccessTools.Field(
                dataType,
                memberName
            );

        if (field is not null &&
            field.FieldType ==
                typeof(bool))
        {
            try
            {
                object? value =
                    field.GetValue(
                        data
                    );

                if (value is bool result &&
                    result)
                {
                    return true;
                }
            }
            catch
            {
                // 继续检查其他成员。
            }
        }
    }

    return false;
}
}

/// <summary>
/// 游戏每次重新计算负重后，
/// 将状态覆盖为菜单指定的数值。
/// </summary>
[HarmonyPatch(
    typeof(CharacterAfflictions),
    "UpdateWeight"
)]
internal static class WeightOverridePatch
{
    private static void Postfix(
        CharacterAfflictions __instance)
    {
        if (!StatusProtectionPatch
                .WeightOverrideEnabled)
        {
            return;
        }

        Character? localCharacter =
            Character.localCharacter;

        if (localCharacter is null)
        {
            return;
        }

        CharacterAfflictions? localAfflictions =
            localCharacter
                .GetComponent<
                    CharacterAfflictions>() ??
            localCharacter
                .GetComponentInChildren<
                    CharacterAfflictions>(
                        true
                    );

        if (localAfflictions is null ||
            !ReferenceEquals(
                __instance,
                localAfflictions))
        {
            return;
        }

        __instance.SetStatus(
            CharacterAfflictions
                .STATUSTYPE
                .Weight,
            StatusProtectionPatch
                .WeightOverrideValue
        );
    }
}