using System;
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
     * 每秒最多发送约 40 次飞行坐标同步。
     */
    private const float WarpInterval =
        0.025f;

    /*
     * 每次成功发送 Warp RPC 后，
     * 将缓降效果续期到未来 5 秒。
     */
    private const float SlowFallDuration =
        5f;

    /*
     * 原生低重力状态的持有量。
     *
     * 可根据实际游戏效果调整。
     */
    private const float NativeSlowFallAmount =
        1f;

    /*
     * 没找到原生低重力状态时，
     * 限制角色最快向下速度。
     */
    private const float FallbackMaximumFallSpeed =
        -2.25f;

    private static readonly
        CharacterAfflictions.STATUSTYPE?
        SlowFallStatusType =
            ResolveSlowFallStatusType();

    private bool _enabled;
    private bool _activelyFlying;

    private bool _doubleTapMode = true;

    /*
     * 默认开启滚轮调速。
     */
    private bool _scrollSpeedEnabled = true;

    /*
     * 默认开启每次 RPC 后的五秒缓降。
     */
    private bool _slowFallEnabled = true;

    private float _flightSpeed =
        DefaultFlightSpeed;

    private float _lastSpacePressTime =
        -100f;

    private float _nextWarpTime;

    private Vector3 _flightPosition;

    private bool _flightPositionInitialized;

    private Character? _activeCharacter;

    private bool _savedInvincible;
    private bool _savedAntiKnockback;
    private bool _hasSavedStatusState;

    /*
     * 缓降到期时间。
     */
    private float _slowFallUntil;

    /*
     * 防止效果到期后每帧重复清零。
     */
    private bool _slowFallWasApplied;

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
         * 缓降可能在关闭飞行后继续存在五秒，
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
            _activeCharacter = null;
            _flightPositionInitialized = false;

            LastSucceeded = false;
            LastStatus =
                "未找到本地玩家，请先进入关卡。";

            return;
        }

        if (!ReferenceEquals(
                _activeCharacter,
                character))
        {
            _activeCharacter = character;
            _flightPositionInitialized = false;

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
            !_activelyFlying ||
            MenuState.IsOpen)
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
            _activeCharacter = character;

            InitializeFlightPosition(
                character
            );
        }

        Vector3 movement =
            ReadMovementDirection(
                character
            );

        if (movement.sqrMagnitude >
            1f)
        {
            movement.Normalize();
        }

        float speed =
            _flightSpeed;

        if (UnityEngine.Input.GetKey(
                KeyCode.LeftShift) ||
            UnityEngine.Input.GetKey(
                KeyCode.RightShift))
        {
            speed *=
                SprintMultiplier;
        }

        _flightPosition +=
            movement *
            speed *
            Time.fixedDeltaTime;

        /*
         * 即使没有移动输入，也继续发送锁定坐标，
         * 从而防止角色因游戏重力下落。
         */
        if (Time.unscaledTime <
            _nextWarpTime)
        {
            return;
        }

        _nextWarpTime =
            Time.unscaledTime +
            WarpInterval;

        if (!TryWarpCharacter(
                character,
                _flightPosition,
                out string error))
        {
            LastSucceeded = false;
            LastStatus =
                $"飞行坐标更新失败：{error}";

            return;
        }

        /*
         * 每次 RPC 成功后续期五秒缓降效果。
         */
        if (_slowFallEnabled)
        {
            RefreshSlowFall(
                character
            );
        }

        LastSucceeded = true;
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

        _doubleTapMode = enabled;
        _lastSpacePressTime = -100f;

        if (!_enabled)
        {
            LastSucceeded = true;

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

            LastSucceeded = true;
            LastStatus =
                "双击模式已开启，目前处于正常状态；双击空格开始飞行。";

            return;
        }

        SetActiveFlight(
            true
        );

        LastSucceeded = true;
        LastStatus =
            "双击模式已关闭，已立即进入飞行状态。";
    }

    internal void SetScrollSpeedEnabled(
        bool enabled)
    {
        _scrollSpeedEnabled =
            enabled;

        LastSucceeded = true;

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

        LastSucceeded = true;

        LastStatus =
            enabled
                ? "飞行缓降效果已开启，每次坐标同步都会续期五秒。"
                : "飞行缓降效果已关闭。";
    }

    internal void SetFlightSpeed(
        float speed)
    {
        if (float.IsNaN(speed) ||
            float.IsInfinity(speed))
        {
            LastSucceeded = false;
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

        LastSucceeded = true;

        LastStatus =
            $"飞行速度已设为 " +
            $"{_flightSpeed:0.##}。";
    }

    internal void ToggleActiveFlight()
    {
        if (!_enabled)
        {
            LastSucceeded = false;
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

        _activeCharacter = null;
        _flightPositionInitialized = false;
    }

    private void EnableFlightSystem()
    {
        if (!StatusRuntime.IsInitialized)
        {
            LastSucceeded = false;

            LastStatus =
                "状态功能尚未初始化，无法开启飞行保护。";

            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            LastSucceeded = false;

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

        _hasSavedStatusState = true;

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

        _enabled = true;
        _activeCharacter = character;

        _lastSpacePressTime = -100f;
        _nextWarpTime = 0f;

        _flightPositionInitialized = false;

        if (_doubleTapMode)
        {
            _activelyFlying = false;

            LastSucceeded = true;

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

        LastSucceeded = true;

        LastStatus =
            "飞行总开关已开启，已进入坐标飞行状态。";

        Plugin.Log.LogInfo(
            "Flight system enabled and coordinate flight activated."
        );
    }

    private void DisableFlightSystem(
        bool restoreStatusState)
    {
        _activelyFlying = false;
        _enabled = false;

        _activeCharacter = null;

        _flightPosition =
            Vector3.zero;

        _flightPositionInitialized =
            false;

        _lastSpacePressTime =
            -100f;

        _nextWarpTime =
            0f;

        /*
         * 这里不清除缓降到期时间。
         *
         * 因此关闭飞行后，
         * 最后一次 RPC 提供的缓降仍会保留五秒。
         */

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

        _hasSavedStatusState = false;

        LastSucceeded = true;

        LastStatus =
            "飞行已关闭，并已恢复此前的无敌与防击退状态。";

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
                LastSucceeded = false;

                LastStatus =
                    "未找到本地玩家，无法进入飞行状态。";

                return;
            }

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
                _activelyFlying = false;

                LastSucceeded = false;
                LastStatus =
                    $"无法开始飞行：{error}";

                return;
            }

            if (_slowFallEnabled)
            {
                RefreshSlowFall(
                    character
                );
            }

            LastSucceeded = true;
            LastStatus =
                "已进入坐标飞行状态。";

            Plugin.Log.LogInfo(
                $"Coordinate flight enabled at " +
                $"{_flightPosition}."
            );

            return;
        }

        _activelyFlying = false;
        _flightPositionInitialized = false;
        _nextWarpTime = 0f;

        LastSucceeded = true;
        LastStatus =
            _slowFallEnabled
                ? "已恢复正常状态，最后一次缓降效果最多保留五秒。"
                : "已恢复正常状态；双击空格可再次飞行。";

        Plugin.Log.LogInfo(
            "Coordinate flight disabled."
        );
    }

    private void InitializeFlightPosition(
        Character character)
    {
        _flightPosition =
            ResolveCharacterWorldPosition(
                character
            );

        _flightPositionInitialized =
            true;

        _nextWarpTime =
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
            Mathf.Sign(scroll);

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

        LastSucceeded = true;

        LastStatus =
            direction > 0f
                ? $"滚轮提高速度：{_flightSpeed:0.##}。"
                : $"滚轮降低速度：{_flightSpeed:0.##}。";
    }

    private void RefreshSlowFall(
        Character character)
    {
        _slowFallUntil =
            Time.unscaledTime +
            SlowFallDuration;

        _slowFallWasApplied =
            true;

        /*
         * 每次 RPC 后立即应用一次，
         * Update 中还会持续维护。
         */
        ApplySlowFall(
            character
        );
    }

    private void MaintainSlowFall()
    {
        if (!_slowFallWasApplied)
        {
            return;
        }

        Character? character =
            Character.localCharacter;

        if (character is null)
        {
            return;
        }

        if (!_slowFallEnabled)
        {
            ClearSlowFallImmediately();
            return;
        }

        if (Time.unscaledTime <
            _slowFallUntil)
        {
            ApplySlowFall(
                character
            );

            return;
        }

        ClearSlowFallImmediately();
    }

    private static void ApplySlowFall(
        Character character)
    {
        /*
         * 优先应用游戏原生低重力类状态。
         */
        if (SlowFallStatusType.HasValue)
        {
            CharacterAfflictions?
                afflictions =
                    FindAfflictions(
                        character
                    );

            if (afflictions is not null)
            {
                try
                {
                    afflictions.SetStatus(
                        SlowFallStatusType.Value,
                        NativeSlowFallAmount
                    );
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogDebug(
                        $"Native slow-fall status failed: " +
                        $"{exception.Message}"
                    );
                }
            }
        }

        /*
         * 无论有没有原生状态，都提供向下速度限制。
         *
         * 这样即使当前 PEAK 版本没有 LowGravity 枚举，
         * 停止飞行后也能得到实际缓降。
         */
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

            Vector3 velocity =
                rigidbody.linearVelocity;

            if (velocity.y >=
                FallbackMaximumFallSpeed)
            {
                continue;
            }

            velocity.y =
                FallbackMaximumFallSpeed;

            rigidbody.linearVelocity =
                velocity;
        }
    }

    private void ClearSlowFallImmediately()
    {
        if (SlowFallStatusType.HasValue)
        {
            Character? character =
                Character.localCharacter;

            CharacterAfflictions?
                afflictions =
                    character is not null
                        ? FindAfflictions(
                            character
                        )
                        : null;

            if (afflictions is not null)
            {
                try
                {
                    afflictions.SetStatus(
                        SlowFallStatusType.Value,
                        0f
                    );
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogDebug(
                        $"Failed to clear native " +
                        $"slow-fall status: " +
                        $"{exception.Message}"
                    );
                }
            }
        }

        _slowFallUntil = 0f;
        _slowFallWasApplied = false;
    }

    private static
        CharacterAfflictions.STATUSTYPE?
        ResolveSlowFallStatusType()
    {
        foreach (
            CharacterAfflictions.STATUSTYPE
                statusType
            in Enum.GetValues(
                typeof(
                    CharacterAfflictions
                        .STATUSTYPE)))
        {
            string name =
                NormalizeName(
                    statusType.ToString()
                );

            bool matches =
                name.Contains("lowgravity") ||
                name.Contains("lowgrav") ||
                name.Contains("balloon") ||
                name.Contains("float");

            if (!matches)
            {
                continue;
            }

            Plugin.Log.LogInfo(
                $"Flight slow-fall status resolved: " +
                $"{statusType}."
            );

            return statusType;
        }

        Plugin.Log.LogWarning(
            "No native LowGravity/Balloon status " +
            "was found. Flight slow-fall will use " +
            "the velocity-limit fallback."
        );

        return null;
    }

    private static string NormalizeName(
        string value)
    {
        return value
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    private static CharacterAfflictions?
        FindAfflictions(
            Character character)
    {
        CharacterAfflictions? result =
            character.GetComponent<
                CharacterAfflictions>();

        if (result is not null)
        {
            return result;
        }

        return character
            .GetComponentInChildren<
                CharacterAfflictions>(
                    true
                );
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

        forward.y = 0f;
        right.y = 0f;

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
            movement += forward;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.S))
        {
            movement -= forward;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.D))
        {
            movement += right;
        }

        if (UnityEngine.Input.GetKey(
                KeyCode.A))
        {
            movement -= right;
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
        error = string.Empty;

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

        if (rigidbodies.Length == 0)
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

        int validCount = 0;

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

            positionSum += position;
            validCount++;
        }

        if (validCount > 0)
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