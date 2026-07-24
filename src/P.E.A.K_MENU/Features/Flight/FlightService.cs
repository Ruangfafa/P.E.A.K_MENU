using System;
using System.Collections.Generic;
using System.Linq;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.UI;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Flight;

/// <summary>
/// 本地玩家物理式飞行。
///
/// 飞行期间不修改角色坐标，
/// 不使用 WarpPlayerRPC，
/// 通过关闭角色刚体重力并修正速度实现飞行。
/// </summary>
internal sealed class FlightService :
    IDisposable
{
    private const float DefaultFlightSpeed =
        16f;

    private const float MinimumFlightSpeed =
        0.5f;

    private const float MaximumFlightSpeed =
        100f;

    private const float SprintMultiplier =
        2f;

    private const float DoubleTapInterval =
        0.30f;

    /*
     * 滚轮每格增加或减少的速度。
     */
    private const float ScrollSpeedStep =
        5f;

    /*
     * 角色速度接近目标速度的加速度。
     *
     * 数值越高，飞行响应越直接。
     */
    private const float FlightAcceleration =
        70f;

    /*
     * 松开移动按键后进入悬停时，
     * 将速度拉回零的加速度。
     */
    private const float HoverAcceleration =
        90f;

    /*
     * 避免绳子、碰撞或游戏自身控制
     * 让角色获得极端速度。
     */
    private const float MaximumSafeSpeed =
        120f;

    /*
     * 退出飞行保护：
     *
     * 前 1 秒无重力；
     * 总计 5 秒缓降。
     */
    private const float ZeroGravityDuration =
        1f;

    private const float SlowFallDuration =
        5f;

    /*
     * 缓降阶段允许的最大向下速度。
     */
    private const float MaximumSlowFallSpeed =
        -2.25f;

    private bool _enabled;
    private bool _activelyFlying;

    private bool _doubleTapMode =
        true;

    private bool _scrollSpeedEnabled =
        true;

    private bool _slowFallEnabled =
        true;

    private float _flightSpeed =
        DefaultFlightSpeed;

    private float _lastSpacePressTime =
        -100f;

    private Character? _activeCharacter;

    private bool _savedInvincible;
    private bool _savedAntiKnockback;
    private bool _hasSavedStatusState;

    private float _slowFallUntil;
    private float _zeroGravityUntil;

    private bool _slowFallWasApplied;

    /*
     * 保存退出飞行缓降开始前，
     * 每个刚体原本的重力状态。
     */
    private readonly Dictionary<Rigidbody, bool>
        _slowFallGravityStates =
            new();

    internal bool Enabled =>
        _enabled;

    internal bool ActivelyFlying =>
        _activelyFlying;

    internal bool DoubleTapMode =>
        _doubleTapMode;

    internal bool ScrollSpeedEnabled =>
        _scrollSpeedEnabled;

    internal bool SlowFallEnabled =>
        _slowFallEnabled;

    internal float FlightSpeed =>
        _flightSpeed;

    internal string LastStatus
    {
        get;
        private set;
    } = "飞行功能尚未开启。";

    internal bool LastSucceeded
    {
        get;
        private set;
    }

    internal void Update()
    {
        /*
         * 缓降可能在关闭飞行总开关后继续生效，
         * 因此必须始终维护。
         */
        MaintainSlowFall();

        if (!_enabled)
        {
            return;
        }

        MaintainStatusProtection();

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            _activeCharacter =
                null;

            LastSucceeded =
                false;

            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (!ReferenceEquals(
                _activeCharacter,
                character))
        {
            _activeCharacter =
                character;

            EnsureFlightController(
                character
            );

            Plugin.Log.LogInfo(
                $"Flight character changed: " +
                $"{character.name}."
            );
        }

        if (MenuState.IsOpen)
        {
            return;
        }

        if (_scrollSpeedEnabled &&
            _activelyFlying)
        {
            ReadMouseWheelSpeed();
        }

        if (_doubleTapMode)
        {
            DetectDoubleSpace();
        }
    }
    
    private static void EnsureFlightController(
        Character character)
    {
        FlightController? controller =
            character.GetComponent<
                FlightController>();

        if (controller is not null)
        {
            return;
        }

        character.gameObject
            .AddComponent<
                FlightController>();

        Plugin.Log.LogInfo(
            $"FlightController added to " +
            $"{character.name} by FlightService."
        );
    }

    internal void SetEnabled(
        bool enabled)
    {
        if (_enabled ==
            enabled)
        {
            return;
        }

        if (enabled)
        {
            EnableFlightSystem();
            return;
        }

        DisableFlightSystem(
            restoreStatusState: true
        );
    }

    internal void SetDoubleTapMode(
        bool enabled)
    {
        if (_doubleTapMode ==
            enabled)
        {
            return;
        }

        _doubleTapMode =
            enabled;

        _lastSpacePressTime =
            -100f;

        if (!_enabled)
        {
            LastSucceeded =
                true;

            LastStatus =
                enabled
                    ? "已选择双击空格切换实际飞行。"
                    : "已选择开启总开关后立即飞行。";

            return;
        }

        if (enabled)
        {
            SetActiveFlight(
                false
            );

            LastSucceeded =
                true;

            LastStatus =
                "双击模式已开启，目前处于正常状态；双击空格开始飞行。";

            return;
        }

        SetActiveFlight(
            true
        );

        LastSucceeded =
            true;

        LastStatus =
            "双击模式已关闭，已立即进入飞行状态。";
    }

    internal void SetScrollSpeedEnabled(
        bool enabled)
    {
        _scrollSpeedEnabled =
            enabled;

        LastSucceeded =
            true;

        LastStatus =
            enabled
                ? "飞行中滚轮调速已开启。"
                : "飞行中滚轮调速已关闭。";
    }

    internal void SetSlowFallEnabled(
        bool enabled)
    {
        _slowFallEnabled =
            enabled;

        if (!enabled)
        {
            ClearSlowFallImmediately();
        }

        LastSucceeded =
            true;

        LastStatus =
            enabled
                ? "退出飞行保护已开启：1 秒无重力，随后缓降至第 5 秒。"
                : "退出飞行保护已关闭。";
    }

    internal void SetFlightSpeed(
        float speed)
    {
        if (float.IsNaN(
                speed) ||
            float.IsInfinity(
                speed))
        {
            LastSucceeded =
                false;

            LastStatus =
                "飞行速度格式无效。";

            return;
        }

        _flightSpeed =
            Mathf.Clamp(
                speed,
                MinimumFlightSpeed,
                MaximumFlightSpeed
            );

        LastSucceeded =
            true;

        LastStatus =
            $"飞行速度已设为 " +
            $"{_flightSpeed:0.##}。";
    }

    internal void ToggleActiveFlight()
    {
        if (!_enabled)
        {
            LastSucceeded =
                false;

            LastStatus =
                "请先开启飞行总开关。";

            return;
        }

        SetActiveFlight(
            !_activelyFlying
        );
    }

    public void Dispose()
    {
        DisableFlightSystem(
            restoreStatusState: true
        );

        ClearSlowFallImmediately();

        _activeCharacter =
            null;
    }

    private void EnableFlightSystem()
    {
        if (!StatusRuntime.IsInitialized)
        {
            LastSucceeded =
                false;

            LastStatus =
                "状态功能尚未初始化，无法开启飞行保护。";

            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            LastSucceeded =
                false;

            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        StatusService statusService =
            StatusRuntime.Service;

        _savedInvincible =
            statusService.Invincible;

        _savedAntiKnockback =
            statusService.AntiKnockback;

        _hasSavedStatusState =
            true;

        statusService.SetFlightProtectionLock(
            true
        );

        statusService.SetInvincible(
            true,
            force: true
        );

        /*
         * 飞行期间关闭防击退，
         * 防止相关补丁清除飞行速度。
         */
        statusService.SetAntiKnockback(
            false,
            force: true
        );

        _enabled =
            true;

        _activeCharacter =
            character;

        _lastSpacePressTime =
            -100f;

        if (_doubleTapMode)
        {
            _activelyFlying =
                false;

            LastSucceeded =
                true;

            LastStatus =
                "飞行总开关已开启。双击空格开始飞行。";

            Plugin.Log.LogInfo(
                "Flight system enabled in standby mode."
            );

            return;
        }

        SetActiveFlight(
            true
        );

        LastSucceeded =
            true;

        LastStatus =
            "飞行总开关已开启，已进入物理飞行状态。";

        Plugin.Log.LogInfo(
            "Flight system enabled and physical flight activated."
        );
    }

    private void DisableFlightSystem(
        bool restoreStatusState)
    {
        Character? character =
            Character.localCharacter;

        bool wasActivelyFlying =
            _activelyFlying;

        _activelyFlying =
            false;

        _enabled =
            false;

        _activeCharacter =
            null;

        _lastSpacePressTime =
            -100f;

        if (wasActivelyFlying &&
            _slowFallEnabled &&
            character is not null &&
            character.IsLocal)
        {
            RefreshSlowFall(
                character
            );
        }

        if (restoreStatusState &&
            _hasSavedStatusState &&
            StatusRuntime.IsInitialized)
        {
            StatusService statusService =
                StatusRuntime.Service;

            statusService.SetFlightProtectionLock(
                false
            );

            statusService.SetInvincible(
                _savedInvincible,
                force: true
            );

            statusService.SetAntiKnockback(
                _savedAntiKnockback,
                force: true
            );
        }
        else if (StatusRuntime.IsInitialized)
        {
            StatusRuntime
                .Service
                .SetFlightProtectionLock(
                    false
                );
        }

        _hasSavedStatusState =
            false;

        LastSucceeded =
            true;

        LastStatus =
            wasActivelyFlying &&
            _slowFallEnabled
                ? "飞行已关闭，已恢复此前状态，并获得 1 秒无重力和总计 5 秒缓降保护。"
                : "飞行已关闭，并已恢复此前的无敌与防击退状态。";

        Plugin.Log.LogInfo(
            "Physical flight system disabled."
        );
    }

    private void SetActiveFlight(
        bool active)
    {
        if (!_enabled)
        {
            return;
        }

        if (_activelyFlying ==
            active)
        {
            return;
        }

        Character? character =
            Character.localCharacter;

        if (active)
        {
            if (character is null ||
                !character.IsLocal)
            {
                LastSucceeded =
                    false;

                LastStatus =
                    "未找到本地玩家，无法进入飞行状态。";

                return;
            }

            ClearSlowFallImmediately();

            _activeCharacter =
                character;

            EnsureFlightController(
                character
            );

            _activelyFlying =
                true;

            TryResetFalling(
                character
            );

            LastSucceeded =
                true;

            LastStatus =
                "已进入物理飞行状态。";

            Plugin.Log.LogInfo(
                "Physical flight enabled."
            );

            return;
        }

        _activelyFlying =
            false;

        if (_slowFallEnabled &&
            character is not null &&
            character.IsLocal)
        {
            RefreshSlowFall(
                character
            );
        }

        LastSucceeded =
            true;

        LastStatus =
            _slowFallEnabled
                ? "已退出飞行：获得 1 秒无重力和总计 5 秒缓降保护。"
                : "已恢复正常状态；双击空格可再次飞行。";

        Plugin.Log.LogInfo(
            "Physical flight disabled."
        );
    }

    private void DetectDoubleSpace()
    {
        if (!UnityEngine.Input.GetKeyDown(
                KeyCode.Space))
        {
            return;
        }

        float currentTime =
            Time.unscaledTime;

        float elapsed =
            currentTime -
            _lastSpacePressTime;

        if (elapsed <=
            DoubleTapInterval)
        {
            _lastSpacePressTime =
                -100f;

            ToggleActiveFlight();

            return;
        }

        _lastSpacePressTime =
            currentTime;
    }

    private void ReadMouseWheelSpeed()
    {
        float scroll =
            UnityEngine.Input
                .mouseScrollDelta
                .y;

        if (Mathf.Abs(
                scroll) <
            0.01f)
        {
            return;
        }

        float direction =
            Mathf.Sign(
                scroll
            );

        float previousSpeed =
            _flightSpeed;

        _flightSpeed =
            Mathf.Clamp(
                _flightSpeed +
                direction *
                ScrollSpeedStep,
                MinimumFlightSpeed,
                MaximumFlightSpeed
            );

        if (Mathf.Approximately(
                previousSpeed,
                _flightSpeed))
        {
            return;
        }

        LastSucceeded =
            true;

        LastStatus =
            direction > 0f
                ? $"滚轮提高速度：{_flightSpeed:0.##}。"
                : $"滚轮降低速度：{_flightSpeed:0.##}。";
    }

    private void RefreshSlowFall(
        Character character)
    {
        RestoreSlowFallGravityStates();

        float currentTime =
            Time.unscaledTime;

        _zeroGravityUntil =
            currentTime +
            ZeroGravityDuration;

        _slowFallUntil =
            currentTime +
            SlowFallDuration;

        Rigidbody[] rigidbodies =
            character.GetComponentsInChildren<
                Rigidbody>(
                    true
                );

        foreach (Rigidbody rigidbody
                 in rigidbodies)
        {
            if (!IsUsableRigidbody(
                    rigidbody))
            {
                continue;
            }

            _slowFallGravityStates[
                rigidbody
            ] =
                rigidbody.useGravity;

            rigidbody.useGravity =
                false;

            Vector3 velocity =
                rigidbody.linearVelocity;

            if (velocity.y <
                0f)
            {
                velocity.y =
                    0f;

                rigidbody.linearVelocity =
                    velocity;
            }
        }

        _slowFallWasApplied =
            _slowFallGravityStates.Count >
            0;

        if (!_slowFallWasApplied)
        {
            _zeroGravityUntil =
                0f;

            _slowFallUntil =
                0f;

            return;
        }

        Plugin.Log.LogInfo(
            $"Flight exit protection applied: " +
            $"{ZeroGravityDuration:0.##} seconds " +
            $"zero gravity and " +
            $"{SlowFallDuration:0.##} seconds " +
            $"total slow fall."
        );
    }

    private void MaintainSlowFall()
    {
        if (!_slowFallWasApplied)
        {
            return;
        }

        if (!_slowFallEnabled)
        {
            ClearSlowFallImmediately();
            return;
        }

        float currentTime =
            Time.unscaledTime;

        if (currentTime >=
            _slowFallUntil)
        {
            ClearSlowFallImmediately();
            return;
        }

        bool zeroGravityActive =
            currentTime <
            _zeroGravityUntil;

        foreach (
            KeyValuePair<Rigidbody, bool>
                entry
            in _slowFallGravityStates
                .ToArray())
        {
            Rigidbody rigidbody =
                entry.Key;

            if (!IsUsableRigidbody(
                    rigidbody))
            {
                _slowFallGravityStates.Remove(
                    rigidbody
                );

                continue;
            }

            if (zeroGravityActive)
            {
                rigidbody.useGravity =
                    false;

                Vector3 velocity =
                    rigidbody.linearVelocity;

                if (velocity.y <
                    0f)
                {
                    velocity.y =
                        0f;

                    rigidbody.linearVelocity =
                        velocity;
                }

                continue;
            }

            rigidbody.useGravity =
                entry.Value;

            Vector3 velocityAfterGravity =
                rigidbody.linearVelocity;

            if (velocityAfterGravity.y <
                MaximumSlowFallSpeed)
            {
                velocityAfterGravity.y =
                    MaximumSlowFallSpeed;

                rigidbody.linearVelocity =
                    velocityAfterGravity;
            }
        }

        if (_slowFallGravityStates.Count >
            0)
        {
            return;
        }

        _zeroGravityUntil =
            0f;

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void ClearSlowFallImmediately()
    {
        RestoreSlowFallGravityStates();

        _zeroGravityUntil =
            0f;

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void RestoreSlowFallGravityStates()
    {
        foreach (
            KeyValuePair<Rigidbody, bool>
                entry
            in _slowFallGravityStates
                .ToArray())
        {
            Rigidbody rigidbody =
                entry.Key;

            if (rigidbody is null)
            {
                continue;
            }

            try
            {
                rigidbody.useGravity =
                    entry.Value;
            }
            catch (Exception exception)
            {
                Plugin.Log.LogDebug(
                    $"Failed to restore slow-fall " +
                    $"gravity: {exception.Message}"
                );
            }
        }

        _slowFallGravityStates.Clear();
    }

    private static Vector3
        ReadMovementDirection(
            Character character)
    {
        Camera? camera =
            Camera.main;

        Vector3 forward;
        Vector3 right;

        if (camera is not null)
        {
            forward =
                camera.transform.forward;

            right =
                camera.transform.right;
        }
        else
        {
            forward =
                character.transform.forward;

            right =
                character.transform.right;
        }

        forward.y =
            0f;

        right.y =
            0f;

        if (forward.sqrMagnitude <
            0.001f)
        {
            forward =
                Vector3.forward;
        }

        if (right.sqrMagnitude <
            0.001f)
        {
            right =
                Vector3.right;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            Vector3.zero;

        if (UnityEngine.Input.GetKey(
                KeyCode.W))
        {
            movement +=
                forward;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.S))
        {
            movement -=
                forward;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.D))
        {
            movement +=
                right;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.A))
        {
            movement -=
                right;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.Space))
        {
            movement +=
                Vector3.up;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.LeftControl) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightControl))
        {
            movement +=
                Vector3.down;
        }

        return movement;
    }

    private static void TryMaintainGroundedState(
        Character character)
    {
        try
        {
            character.data.isGrounded =
                true;
        }
        catch (Exception exception)
        {
            Plugin.Log.LogDebug(
                $"Failed to maintain grounded state: " +
                $"{exception.Message}"
            );
        }
    }

    private static bool IsUsableRigidbody(
        Rigidbody? rigidbody)
    {
        if (rigidbody is null)
        {
            return false;
        }

        GameObject gameObject =
            rigidbody.gameObject;

        return
            gameObject is not null &&
            gameObject.scene.IsValid() &&
            gameObject.activeInHierarchy;
    }

    private static bool IsFiniteVector(
        Vector3 value)
    {
        return
            !float.IsNaN(value.x) &&
            !float.IsNaN(value.y) &&
            !float.IsNaN(value.z) &&
            !float.IsInfinity(value.x) &&
            !float.IsInfinity(value.y) &&
            !float.IsInfinity(value.z);
    }

    private static void TryResetFalling(
        Character character)
    {
        try
        {
            character.RPCA_UnFall();
        }
        catch (Exception exception)
        {
            Plugin.Log.LogDebug(
                $"RPCA_UnFall failed while " +
                $"starting flight: " +
                $"{exception.Message}"
            );
        }
    }

    private void MaintainStatusProtection()
    {
        if (!StatusRuntime.IsInitialized)
        {
            return;
        }

        StatusService service =
            StatusRuntime.Service;

        if (!service.FlightProtectionLock)
        {
            service.SetFlightProtectionLock(
                true
            );
        }

        if (!service.Invincible)
        {
            service.SetInvincible(
                true,
                force: true
            );
        }

        /*
         * 物理飞行期间保持防击退关闭，
         * 避免补丁移除飞行速度。
         */
        if (service.AntiKnockback)
        {
            service.SetAntiKnockback(
                false,
                force: true
            );
        }
    }
}