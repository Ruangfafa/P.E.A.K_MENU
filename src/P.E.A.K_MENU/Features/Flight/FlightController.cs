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
        110000f;

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

        float hoverDownForce =
            FlightRuntime
                .Service
                .HoverDownForce;

        float force =
            selectedSpeed *
            FlightForceMultiplier;

        Vector3 flyForce =
            Vector3.zero;

        Vector3 lookDirection =
            ResolveLookDirection(
                character
            );

        Vector3 forwardDirection =
            FlightRuntime
                .Service
                .HorizontalWasdMovement
                ? ResolveHorizontalForwardDirection(
                    character,
                    lookDirection
                )
                : lookDirection;

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
                forwardDirection *
                force;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.S))
        {
            flyForce -=
                forwardDirection *
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
         * 否则重力校准值也会被放大四倍。
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

        flyForce =
            ClampForce(
                flyForce
            );

        if (!IsFiniteVector(
                flyForce))
        {
            return;
        }

        /*
         * 移动力按质量分配，避免轻量四肢获得远高于躯干的
         * 加速度后先撞上障碍物并把整体航向拖偏。
         */
        ApplyMassWeightedForceToRagdoll(
            character,
            flyForce
        );

        /*
         * 悬停补偿继续沿用 0.3.4 的逐部位施力方式，
         * 保持现有的 380 校准手感。
         */
        ApplyForceToRagdoll(
            character,
            Vector3.down *
            hoverDownForce
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
                character.refs.ragdoll.partList is null)
            {
                return;
            }

            int partCount = 0;
            float totalMass = 0f;

            foreach (var part in
                     character.refs.ragdoll.partList)
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
                ApplyForceToRagdoll(
                    character,
                    force
                );

                return;
            }

            float massWeightScale =
                partCount / totalMass;

            foreach (var part in
                     character.refs.ragdoll.partList)
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
                $"Failed to apply mass-weighted flight force: " +
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

    private static Vector3
        ResolveHorizontalForwardDirection(
            Character character,
            Vector3 lookDirection)
    {
        Vector3 horizontalDirection =
            lookDirection;

        horizontalDirection.y = 0f;

        if (!IsFiniteVector(horizontalDirection) ||
            horizontalDirection.sqrMagnitude < 0.001f)
        {
            horizontalDirection =
                character.transform.forward;

            horizontalDirection.y = 0f;
        }

        if (!IsFiniteVector(horizontalDirection) ||
            horizontalDirection.sqrMagnitude < 0.001f)
        {
            horizontalDirection =
                Vector3.forward;
        }

        horizontalDirection.Normalize();

        return horizontalDirection;
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
