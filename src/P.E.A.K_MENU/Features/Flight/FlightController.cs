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
     * 防止异常输入或速度设置产生过大的作用力。
     */
    private const float MaximumForce =
        60000f;

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
         * 仍保留原有悬停补偿，避免角色下沉。
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

        float hoverDownForce =
            FlightRuntime
                .Service
                .HoverDownForce;

        float force =
            selectedSpeed *
            FlightForceMultiplier;

        Vector3 forwardDirection =
            ResolveHorizontalForwardDirection(
                character
            );

        /*
         * WASD 只根据视角的水平朝向移动。
         * 摄像机俯仰不会再改变飞行高度。
         */
        Vector3 rightDirection =
            ResolveRightDirection(
                character,
                forwardDirection
            );

        float forwardInput =
            (UnityEngine.Input.GetKey(KeyCode.W) ? 1f : 0f) -
            (UnityEngine.Input.GetKey(KeyCode.S) ? 1f : 0f);

        float rightInput =
            (UnityEngine.Input.GetKey(KeyCode.D) ? 1f : 0f) -
            (UnityEngine.Input.GetKey(KeyCode.A) ? 1f : 0f);

        Vector3 horizontalDirection =
            forwardDirection * forwardInput +
            rightDirection * rightInput;

        /*
         * 防止斜向飞行比单方向更快。
         */
        if (horizontalDirection.sqrMagnitude > 1f)
        {
            horizontalDirection.Normalize();
        }

        Vector3 horizontalForce =
            horizontalDirection * force;

        Vector3 verticalForce =
            Vector3.zero;

        /*
         * Space 上升。
         * Ctrl 下降。
         */
        if (UnityEngine.Input.GetKey(
                KeyCode.Space))
        {
            verticalForce +=
                Vector3.up *
                force;
        }
        else if (
            UnityEngine.Input.GetKey(
                KeyCode.LeftControl) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightControl))
        {
            verticalForce +=
                Vector3.down *
                force;
        }

        /*
         * Shift 加速。
         *
         * 必须在加入悬停向下补偿之前处理，
         * 否则重力校准值也会被放大四倍。
         */
        if (UnityEngine.Input.GetKey(
                KeyCode.LeftShift) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightShift))
        {
            horizontalForce *=
                SprintForceMultiplier;

            verticalForce *=
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
         * 默认悬停向下补偿为 237；
         * 因此按 Space 时仍有明显净向上力。
         */
        verticalForce +=
            Vector3.down *
            hoverDownForce;

        horizontalForce =
            ClampForce(
                horizontalForce
            );

        verticalForce =
            ClampForce(
                verticalForce
            );

        if (!IsFiniteVector(
                horizontalForce) ||
            !IsFiniteVector(
                verticalForce))
        {
            return;
        }

        ApplyMassWeightedForceToRagdoll(
            character,
            horizontalForce
        );

        /*
         * 垂直力保留原有的逐部位施力方式，
         * 避免改变已经校准好的悬停、上升和下降手感。
         */
        ApplyForceToRagdoll(
            character,
            verticalForce
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
            FlightRuntime
                .Service
                .HoverDownForce;

        ApplyForceToRagdoll(
            character,
            hoverForce
        );
    }

    private static void ApplyForceToRagdoll(
        Character character,
        Vector3 force)
    {
        if (!IsFiniteVector(force))
        {
            return;
        }

        try
        {
            if (character.refs is null ||
                character.refs.ragdoll is null ||
                character.refs.ragdoll.partList is null)
            {
                return;
            }

            foreach (Bodypart part
                     in character.refs.ragdoll.partList)
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

    private static void ApplyMassWeightedForceToRagdoll(
        Character character,
        Vector3 force)
    {
        if (!IsFiniteVector(force) ||
            force.sqrMagnitude < 0.001f)
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

            int partCount = 0;
            float totalMass = 0f;

            foreach (Bodypart part
                     in character.refs.ragdoll.partList)
            {
                Rigidbody? rigidbody =
                    ResolveRigidbody(part);

                if (!IsUsableRigidbody(rigidbody))
                {
                    continue;
                }

                partCount++;
                totalMass += rigidbody!.mass;
            }

            if (partCount == 0 ||
                totalMass <= 0f ||
                float.IsNaN(totalMass) ||
                float.IsInfinity(totalMass))
            {
                /*
                 * 无法读取质量时退回原有施力逻辑，
                 * 确保水平飞行不会失效。
                 */
                ApplyForceToRagdoll(
                    character,
                    force
                );

                return;
            }

            /*
             * 保持施加到整套布娃娃上的总力不变，
             * 但按质量分配到各部位。
             * 这样轻量四肢不会获得远高于身体的加速度，
             * 从而减少肢体摆动对飞行轨迹的影响。
             */
            float massWeightScale =
                partCount / totalMass;

            foreach (Bodypart part
                     in character.refs.ragdoll.partList)
            {
                Rigidbody? rigidbody =
                    ResolveRigidbody(part);

                if (!IsUsableRigidbody(rigidbody))
                {
                    continue;
                }

                part.AddForce(
                    force *
                    rigidbody!.mass *
                    massWeightScale,
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
        ResolveHorizontalForwardDirection(
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

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <
            0.001f)
        {
            lookDirection =
                character.transform.forward;

            lookDirection.y = 0f;
        }

        if (!IsFiniteVector(lookDirection) ||
            lookDirection.sqrMagnitude < 0.001f)
        {
            lookDirection = Vector3.forward;
        }

        lookDirection.Normalize();

        return lookDirection;
    }

    private static Vector3
        ResolveRightDirection(
            Character character,
            Vector3 horizontalLook)
    {
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

    private static Rigidbody? ResolveRigidbody(
        Bodypart? part)
    {
        return part is null
            ? null
            : part.GetComponent<Rigidbody>();
    }

    private static bool IsUsableRigidbody(
        Rigidbody? rigidbody)
    {
        return
            rigidbody is not null &&
            rigidbody.gameObject is not null &&
            rigidbody.gameObject.activeInHierarchy &&
            rigidbody.mass > 0f &&
            !float.IsNaN(rigidbody.mass) &&
            !float.IsInfinity(rigidbody.mass);
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
