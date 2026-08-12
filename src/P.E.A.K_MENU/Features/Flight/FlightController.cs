using P.E.A.K_MENU.UI;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Flight;

internal sealed class FlightController :
    MonoBehaviour
{
    /*
     * 菜单速度 16 时：
     *
     * 16 × 50 = 800
     */
    private const float FlightForceMultiplier =
        50f;

    /*
     * Shift 加速倍率。
     */
    private const float SprintForceMultiplier =
        4f;

    /*
     * 抵消持续设置 isGrounded 后，
     * PEAK 主动布娃娃产生的向上支撑力。
     *
     * 仍然自动上升时可提高到 120～160；
     * 自动下降时可降低到 60～80。
     */
    private const float HoverDownForce =
        225f;

    /*
     * 防止异常输入或速度设置产生过大的作用力。
     */
    private const float MaximumForce =
        4000f;

    private Character? _character;

    private CharacterMovement?
        _characterMovement;

    private void Awake()
    {
        _character =
            GetComponent<Character>();

        _characterMovement =
            GetComponent<
                CharacterMovement>();

        if (_character is null)
        {
            Plugin.Log.LogWarning(
                "FlightController could not find Character component."
            );

            enabled =
                false;

            return;
        }

        Plugin.Log.LogInfo(
            $"FlightController initialized on " +
            $"{_character.name}."
        );
    }

    private void Update()
    {
        Character? character =
            _character;

        if (character is null ||
            !character.IsLocal)
        {
            return;
        }

        if (!FlightRuntime
                .Service
                .ActivelyFlying)
        {
            return;
        }

        /*
         * 菜单打开时不读取飞行按键。
         *
         * 当前仍然不施加任何飞行力，
         * 角色可能受到少量物理影响。
         */
        if (MenuState.IsOpen)
        {
            ApplyHoverCompensation(
                character
            );

            return;
        }

        ApplyFlight(
            character
        );
    }

    private void ApplyFlight(
        Character character)
    {
        float selectedSpeed =
            FlightRuntime
                .Service
                .FlightSpeed;

        float force =
            selectedSpeed *
            FlightForceMultiplier;

        Vector3 flyForce =
            Vector3.zero;

        Vector3 lookDirection =
            ResolveLookDirection(
                character
            );

        /*
         * W / S 根据视角方向前后移动。
         *
         * 因为保留视角的 Y 分量，
         * 抬头按 W 时也可以向上飞。
         */
        if (UnityEngine.Input.GetKey(
                KeyCode.W))
        {
            flyForce +=
                lookDirection *
                force;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.S))
        {
            flyForce -=
                lookDirection *
                force;
        }

        Vector3 rightDirection =
            ResolveRightDirection(
                character,
                lookDirection
            );

        if (UnityEngine.Input.GetKey(
                KeyCode.D))
        {
            flyForce +=
                rightDirection *
                force;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.A))
        {
            flyForce -=
                rightDirection *
                force;
        }

        /*
         * Space 上升。
         * Ctrl 下降。
         */
        if (UnityEngine.Input.GetKey(
                KeyCode.Space))
        {
            flyForce +=
                Vector3.up *
                force;
        }
        else if (
            UnityEngine.Input.GetKey(
                KeyCode.LeftControl) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightControl))
        {
            flyForce +=
                Vector3.down *
                force;
        }

        /*
         * Shift 加速。
         *
         * 必须在加入悬停向下补偿之前处理，
         * 否则 HoverDownForce 也会被放大四倍。
         */
        if (UnityEngine.Input.GetKey(
                KeyCode.LeftShift) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightShift))
        {
            flyForce *=
                SprintForceMultiplier;

            RestoreSprintStamina(
                character
            );
        }

        /*
         * 阻止 PEAK 进入普通坠落状态。
         */
        TryMaintainGroundedState(
            character
        );

        /*
         * 抵消 isGrounded 带来的自动上升。
         *
         * 默认速度 16 时：
         * Space 向上力约为 800；
         * 悬停向下补偿仅为 100；
         * 因此按 Space 时仍有明显净向上力。
         */
        flyForce +=
            Vector3.down *
            HoverDownForce;

        flyForce =
            ClampForce(
                flyForce
            );

        if (!IsFiniteVector(
                flyForce))
        {
            return;
        }

        ApplyForceToRagdoll(
            character,
            flyForce
        );
    }

    private static void ApplyHoverCompensation(
        Character character)
    {
        TryMaintainGroundedState(
            character
        );

        Vector3 hoverForce =
            Vector3.down *
            HoverDownForce;

        ApplyForceToRagdoll(
            character,
            hoverForce
        );
    }

    private static void ApplyForceToRagdoll(
        Character character,
        Vector3 force)
    {
        if (!IsFiniteVector(
                force))
        {
            return;
        }

        try
        {
            if (character.refs is null ||
                character.refs.ragdoll is null ||
                character
                    .refs
                    .ragdoll
                    .partList is null)
            {
                return;
            }

            foreach (var part
                     in character
                         .refs
                         .ragdoll
                         .partList)
            {
                if (part is null)
                {
                    continue;
                }

                part.AddForce(
                    force,
                    ForceMode.Force
                );
            }
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogDebug(
                $"Failed to apply flight force: " +
                $"{exception.GetBaseException().Message}"
            );
        }
    }

    private static Vector3
        ResolveLookDirection(
            Character character)
    {
        Vector3 lookDirection =
            character
                .data
                .lookDirection;

        if (!IsFiniteVector(
                lookDirection) ||
            lookDirection.sqrMagnitude <
            0.001f)
        {
            lookDirection =
                character
                    .transform
                    .forward;
        }

        if (!IsFiniteVector(
                lookDirection) ||
            lookDirection.sqrMagnitude <
            0.001f)
        {
            lookDirection =
                Vector3.forward;
        }

        lookDirection.Normalize();

        return lookDirection;
    }

    private static Vector3
        ResolveRightDirection(
            Character character,
            Vector3 lookDirection)
    {
        /*
         * 左右移动只使用水平朝向。
         */
        Vector3 horizontalLook =
            lookDirection;

        horizontalLook.y =
            0f;

        if (!IsFiniteVector(
                horizontalLook) ||
            horizontalLook.sqrMagnitude <
            0.001f)
        {
            horizontalLook =
                character
                    .transform
                    .forward;

            horizontalLook.y =
                0f;
        }

        if (!IsFiniteVector(
                horizontalLook) ||
            horizontalLook.sqrMagnitude <
            0.001f)
        {
            horizontalLook =
                Vector3.forward;
        }

        horizontalLook.Normalize();

        Vector3 rightDirection =
            Vector3.Cross(
                Vector3.up,
                horizontalLook
            );

        if (!IsFiniteVector(
                rightDirection) ||
            rightDirection.sqrMagnitude <
            0.001f)
        {
            rightDirection =
                character
                    .transform
                    .right;

            rightDirection.y =
                0f;
        }

        if (!IsFiniteVector(
                rightDirection) ||
            rightDirection.sqrMagnitude <
            0.001f)
        {
            rightDirection =
                Vector3.right;
        }

        rightDirection.Normalize();

        return rightDirection;
    }

    private void RestoreSprintStamina(
        Character character)
    {
        CharacterMovement? movement =
            _characterMovement;

        if (movement is null)
        {
            return;
        }

        try
        {
            character.AddStamina(
                movement
                    .sprintStaminaUsage *
                Time.deltaTime
            );
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogDebug(
                $"Failed to restore flight sprint stamina: " +
                $"{exception.Message}"
            );
        }
    }

    private static void TryMaintainGroundedState(
        Character character)
    {
        try
        {
            character.data.isGrounded =
                true;
        }
        catch (System.Exception exception)
        {
            Plugin.Log.LogDebug(
                $"Failed to maintain flight grounded state: " +
                $"{exception.Message}"
            );
        }
    }

    private static Vector3 ClampForce(
        Vector3 force)
    {
        force.x =
            Mathf.Clamp(
                force.x,
                -MaximumForce,
                MaximumForce
            );

        force.y =
            Mathf.Clamp(
                force.y,
                -MaximumForce,
                MaximumForce
            );

        force.z =
            Mathf.Clamp(
                force.z,
                -MaximumForce,
                MaximumForce
            );

        return force;
    }

    private static bool IsFiniteVector(
        Vector3 value)
    {
        return
            !float.IsNaN(
                value.x) &&
            !float.IsNaN(
                value.y) &&
            !float.IsNaN(
                value.z) &&
            !float.IsInfinity(
                value.x) &&
            !float.IsInfinity(
                value.y) &&
            !float.IsInfinity(
                value.z);
    }
}
