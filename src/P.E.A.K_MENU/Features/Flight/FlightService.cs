using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using P.E.A.K_MENU.Features.Status;
using P.E.A.K_MENU.UI;
using UnityEngine;

namespace P.E.A.K_MENU.Features.Flight;

/// <summary>
/// 本地玩家坐标式飞行。
///
/// 通过 WarpPlayerRPC 直接改变角色坐标，
/// 不使用推力模拟飞行。
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
     * 每秒最多发送约 15 次飞行坐标同步。
     *
     * 较低频率可以减少 Photon RPC 压力，
     * 并改善低性能设备上的稳定性。
     */
    private const float WarpInterval =
        0.067f;

    /*
     * 退出飞行后：
     *
     * 前 1 秒完全关闭角色刚体重力；
     * 总计 5 秒保持缓降保护。
     */
    private const float ZeroGravityDuration =
        1f;

    private const float SlowFallDuration =
        5f;

    /*
     * 无重力结束后，
     * 向下速度最多为 -2.25。
     */
    private const float MaximumSlowFallSpeed =
        -2.25f;

    private bool _enabled;
    private bool _activelyFlying;

    private bool _doubleTapMode =
        true;

    /*
     * 默认开启滚轮调速。
     */
    private bool _scrollSpeedEnabled =
        true;

    /*
     * 默认开启退出飞行保护。
     */
    private bool _slowFallEnabled =
        true;

    private float _flightSpeed =
        DefaultFlightSpeed;

    private float _lastSpacePressTime =
        -100f;

    /*
     * 飞行坐标同步时间累计器。
     *
     * 每次发送后直接清零，
     * 不补偿卡顿期间遗漏的同步。
     */
    private float _warpAccumulator;

    private Vector3 _flightPosition;

    private bool _flightPositionInitialized;

    private Character? _activeCharacter;

    private bool _savedInvincible;
    private bool _savedAntiKnockback;
    private bool _hasSavedStatusState;

    /*
     * 总计 5 秒缓降的到期时间。
     */
    private float _slowFallUntil;

    /*
     * 前 1 秒无重力的到期时间。
     */
    private float _zeroGravityUntil;

    /*
     * 当前是否存在退出飞行保护。
     */
    private bool _slowFallWasApplied;

    /*
     * 保存进入无重力前，
     * 每个刚体原本的 useGravity 状态。
     */
    private readonly Dictionary<Rigidbody, bool>
        _savedGravityStates =
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
         * 退出飞行保护可能在关闭飞行系统后继续存在，
         * 因此必须在 _enabled 判断之前维护。
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

            _flightPositionInitialized =
                false;

            _warpAccumulator =
                0f;

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

            _flightPositionInitialized =
                false;

            _warpAccumulator =
                0f;

            if (_activelyFlying)
            {
                InitializeFlightPosition(
                    character
                );
            }

            Plugin.Log.LogInfo(
                $"Flight character changed: " +
                $"{character.name}."
            );
        }

        /*
         * 菜单打开时不处理键盘切换和滚轮调速。
         *
         * FixedUpdate 仍然会继续维持飞行坐标，
         * 因此打开菜单时不会摔落。
         */
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

    internal void FixedUpdate()
    {
        if (!_enabled ||
            !_activelyFlying)
        {
            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null ||
            !character.IsLocal)
        {
            return;
        }

        if (!_flightPositionInitialized ||
            !ReferenceEquals(
                _activeCharacter,
                character))
        {
            _activeCharacter =
                character;

            InitializeFlightPosition(
                character
            );
        }

        /*
         * 菜单打开时仍然持续锁定飞行坐标，
         * 但不读取任何移动按键。
         */
        Vector3 movement =
            MenuState.IsOpen
                ? Vector3.zero
                : ReadMovementDirection(
                    character
                );

        if (movement.sqrMagnitude >
            1f)
        {
            movement.Normalize();
        }

        float speed =
            _flightSpeed;

        if (!MenuState.IsOpen &&
            (UnityEngine.Input.GetKey(
                 KeyCode.LeftShift) ||
             UnityEngine.Input.GetKey(
                 KeyCode.RightShift)))
        {
            speed *=
                SprintMultiplier;
        }

        _flightPosition +=
            movement *
            speed *
            Time.fixedDeltaTime;

        /*
         * 即使没有移动输入，
         * 也继续发送锁定坐标，
         * 防止角色受到重力影响。
         */
        _warpAccumulator +=
            Time.fixedDeltaTime;

        if (_warpAccumulator <
            WarpInterval)
        {
            return;
        }

        /*
         * 发送一次后直接清零。
         *
         * 不补发卡顿期间遗漏的坐标同步，
         * 防止低性能设备恢复后产生 RPC 堆积。
         */
        _warpAccumulator =
            0f;

        if (!TryWarpCharacter(
                character,
                _flightPosition,
                out string error))
        {
            LastSucceeded =
                false;

            LastStatus =
                $"飞行坐标更新失败：{error}";

            return;
        }

        LastSucceeded =
            true;
    }

    internal void SetEnabled(
        bool enabled)
    {
        if (_enabled == enabled)
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
        if (_doubleTapMode == enabled)
        {
            return;
        }

        _doubleTapMode =
            enabled;

        _lastSpacePressTime =
            -100f;

        _warpAccumulator =
            0f;

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
        if (float.IsNaN(speed) ||
            float.IsInfinity(speed))
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

        _flightPositionInitialized =
            false;

        _warpAccumulator =
            0f;
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

        statusService.SetAntiKnockback(
            true,
            force: true
        );

        _enabled =
            true;

        _activeCharacter =
            character;

        _lastSpacePressTime =
            -100f;

        _warpAccumulator =
            0f;

        _flightPositionInitialized =
            false;

        if (_doubleTapMode)
        {
            _activelyFlying =
                false;

            LastSucceeded =
                true;

            LastStatus =
                "飞行总开关已开启。双击空格开始飞行。";

            Plugin.Log.LogInfo(
                "Flight system enabled in double-tap standby mode."
            );

            return;
        }

        SetActiveFlight(
            true
        );

        LastSucceeded =
            true;

        LastStatus =
            "飞行总开关已开启，已进入坐标飞行状态。";

        Plugin.Log.LogInfo(
            "Flight system enabled and coordinate flight activated."
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

        _flightPosition =
            Vector3.zero;

        _flightPositionInitialized =
            false;

        _lastSpacePressTime =
            -100f;

        _warpAccumulator =
            0f;

        /*
         * 如果关闭总开关时仍处于实际飞行状态，
         * 同样给予退出飞行保护。
         */
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
            "Coordinate flight system disabled."
        );
    }

    private void SetActiveFlight(
        bool active)
    {
        if (!_enabled)
        {
            return;
        }

        if (_activelyFlying == active)
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

            /*
             * 如果上一次退出保护仍在生效，
             * 重新进入飞行时先恢复原始重力状态。
             */
            ClearSlowFallImmediately();

            _activeCharacter =
                character;

            InitializeFlightPosition(
                character
            );

            _activelyFlying =
                true;

            TryResetFalling(
                character
            );

            if (!TryWarpCharacter(
                    character,
                    _flightPosition,
                    out string error))
            {
                _activelyFlying =
                    false;

                _flightPositionInitialized =
                    false;

                _warpAccumulator =
                    0f;

                LastSucceeded =
                    false;

                LastStatus =
                    $"无法开始飞行：{error}";

                return;
            }

            LastSucceeded =
                true;

            LastStatus =
                "已进入坐标飞行状态。";

            Plugin.Log.LogInfo(
                $"Coordinate flight enabled at " +
                $"{_flightPosition}."
            );

            return;
        }

        _activelyFlying =
            false;

        _flightPositionInitialized =
            false;

        _warpAccumulator =
            0f;

        /*
         * 离开坐标飞行时：
         *
         * 前 1 秒无重力，
         * 随后缓降至第 5 秒。
         */
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
            "Coordinate flight disabled."
        );
    }

    private void InitializeFlightPosition(
        Character character)
    {
        Vector3 resolvedPosition =
            ResolveCharacterWorldPosition(
                character
            );

        if (!IsFinitePosition(
                resolvedPosition))
        {
            resolvedPosition =
                character
                    .transform
                    .position;
        }

        _flightPosition =
            resolvedPosition;

        _flightPositionInitialized =
            true;

        _warpAccumulator =
            0f;

        Plugin.Log.LogInfo(
            $"Initialized flight position: " +
            $"{_flightPosition}."
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

        if (Mathf.Abs(scroll) <
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
        /*
         * 先恢复上一次保护保存的状态，
         * 避免重复应用后保存错误的 useGravity 值。
         */
        RestoreGravityStates();

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

            _savedGravityStates[
                rigidbody
            ] =
                rigidbody.useGravity;

            rigidbody.useGravity =
                false;

            /*
             * 清除退出飞行瞬间残留的向下速度。
             */
            Vector3 velocity =
                rigidbody.linearVelocity;

            if (velocity.y < 0f)
            {
                velocity.y =
                    0f;

                rigidbody.linearVelocity =
                    velocity;
            }
        }

        _slowFallWasApplied =
            _savedGravityStates.Count >
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
            $"total slow fall to " +
            $"{_savedGravityStates.Count} rigidbodies."
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

        /*
         * 总计 5 秒结束后恢复全部原始重力状态。
         */
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
            in _savedGravityStates
                .ToArray())
        {
            Rigidbody rigidbody =
                entry.Key;

            if (!IsUsableRigidbody(
                    rigidbody))
            {
                _savedGravityStates.Remove(
                    rigidbody
                );

                continue;
            }

            if (zeroGravityActive)
            {
                /*
                 * 前 1 秒完全关闭重力，
                 * 并阻止继续产生向下速度。
                 */
                rigidbody.useGravity =
                    false;

                Vector3 velocity =
                    rigidbody.linearVelocity;

                if (velocity.y < 0f)
                {
                    velocity.y =
                        0f;

                    rigidbody.linearVelocity =
                        velocity;
                }

                continue;
            }

            /*
             * 第 1～5 秒恢复该刚体原本的重力状态，
             * 但限制最大向下速度，形成缓降。
             */
            rigidbody.useGravity =
                entry.Value;

            Vector3 slowFallVelocity =
                rigidbody.linearVelocity;

            if (slowFallVelocity.y <
                MaximumSlowFallSpeed)
            {
                slowFallVelocity.y =
                    MaximumSlowFallSpeed;

                rigidbody.linearVelocity =
                    slowFallVelocity;
            }
        }

        if (_savedGravityStates.Count >
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
        RestoreGravityStates();

        _zeroGravityUntil =
            0f;

        _slowFallUntil =
            0f;

        _slowFallWasApplied =
            false;
    }

    private void RestoreGravityStates()
    {
        foreach (
            KeyValuePair<Rigidbody, bool>
                entry
            in _savedGravityStates
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
                    $"Failed to restore rigidbody " +
                    $"gravity: {exception.Message}"
                );
            }
        }

        _savedGravityStates.Clear();
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

    private static bool TryWarpCharacter(
        Character character,
        Vector3 destination,
        out string error)
    {
        error =
            string.Empty;

        PhotonView? photonView =
            character.photonView;

        if (photonView is null)
        {
            error =
                "角色 PhotonView 不存在。";

            return false;
        }

        if (!IsFinitePosition(
                destination))
        {
            error =
                "目标坐标无效。";

            return false;
        }

        try
        {
            photonView.RPC(
                "WarpPlayerRPC",
                RpcTarget.All,
                destination,
                false
            );

            return true;
        }
        catch (Exception exception)
        {
            error =
                exception.Message;

            Plugin.Log.LogError(
                $"Flight WarpPlayerRPC failed: " +
                $"{exception}"
            );

            return false;
        }
    }

    private static Vector3
        ResolveCharacterWorldPosition(
            Character character)
    {
        Rigidbody[] rigidbodies =
            character.GetComponentsInChildren<
                Rigidbody>(
                    true
                );

        if (rigidbodies.Length ==
            0)
        {
            return character
                .transform
                .position;
        }

        string[] preferredNames =
        {
            "hip",
            "hips",
            "pelvis",
            "torso",
            "chest",
            "body"
        };

        foreach (string preferredName
                 in preferredNames)
        {
            foreach (Rigidbody rigidbody
                     in rigidbodies)
            {
                if (!IsUsableRigidbody(
                        rigidbody))
                {
                    continue;
                }

                string bodyName =
                    rigidbody.name ??
                    string.Empty;

                if (!bodyName.Contains(
                        preferredName,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    continue;
                }

                Vector3 position =
                    rigidbody
                        .worldCenterOfMass;

                if (IsFinitePosition(
                        position))
                {
                    return position;
                }
            }
        }

        Vector3 positionSum =
            Vector3.zero;

        int validCount =
            0;

        foreach (Rigidbody rigidbody
                 in rigidbodies)
        {
            if (!IsUsableRigidbody(
                    rigidbody))
            {
                continue;
            }

            Vector3 position =
                rigidbody
                    .worldCenterOfMass;

            if (!IsFinitePosition(
                    position))
            {
                continue;
            }

            positionSum +=
                position;

            validCount++;
        }

        if (validCount >
            0)
        {
            return
                positionSum /
                validCount;
        }

        return character
            .transform
            .position;
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

    private static bool IsFinitePosition(
        Vector3 position)
    {
        return
            !float.IsNaN(position.x) &&
            !float.IsNaN(position.y) &&
            !float.IsNaN(position.z) &&
            !float.IsInfinity(position.x) &&
            !float.IsInfinity(position.y) &&
            !float.IsInfinity(position.z);
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

        if (!service.AntiKnockback)
        {
            service.SetAntiKnockback(
                true,
                force: true
            );
        }
    }
}